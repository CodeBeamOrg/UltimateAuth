const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const scriptPath = path.resolve(
    __dirname,
    "../../../src/client/CodeBeam.UltimateAuth.Client.Blazor/TScripts/uauth.js"
);

const script = fs.readFileSync(scriptPath, "utf8");

function createEnvironment(fetchImpl = async () => ({
    ok: true,
    status: 200,
    headers: {
        get: () => "application/json"
    },
    text: async () => '{"id":1}'
}), onNavigate = () => { }) {
    const memoryStorage = () => {
        const values = new Map();
        return { setItem: (key, value) => values.set(key, value), getItem: key => values.get(key) ?? null, removeItem: key => values.delete(key) };
    };
    const window = {
        localStorage: memoryStorage(),
        sessionStorage: memoryStorage(),
        location: {
            origin: "https://localhost:6130",
            assign: url => onNavigate(url)
        }
    };

    const document = {
        baseURI: "https://localhost:6130/",
        createElement: () => ({
            type: "",
            name: "",
            value: ""
        })
    };

    const context = vm.createContext({
        window,
        document,
        fetch: fetchImpl,
        URL,
        URLSearchParams,
        AbortController,
        Map,
        Error,
        TypeError,
        HTMLFormElement: TestForm,
        FormData: TestFormData
    });

    vm.runInContext(script, context);

    window.uauth.setDeviceId("test-device");

    return window.uauth;
}

const HUB_ORIGIN = "https://localhost:6110";

function configureAntiforgery(uauth) {
    uauth.antiforgery.configure({
        tokenUrl: `${HUB_ORIGIN}/auth/antiforgery/token`,
        trustedOrigin: HUB_ORIGIN
    });
}

function authUrl(path) {
    return `${HUB_ORIGIN}${path}`;
}


function csrfAwareFetch(postHandler) {
    return async (url, options = {}) => {
        if (url === authUrl("/auth/antiforgery/token")) {
            assert.equal(options.method, "GET");
            assert.equal(options.credentials, "include");

            return {
                ok: true,
                status: 200,
                json: async () => ({
                    requestToken: "csrf-test-token",
                    headerName: "X-CSRF-TOKEN",
                    formFieldName: "__RequestVerificationToken"
                })
            };
        }

        assert.equal(options.method, "POST");
        assert.equal(
            options.headers["X-CSRF-TOKEN"],
            "csrf-test-token"
        );

        return postHandler(url, options);
    };
}

test("validateAuthUrl accepts trusted origin", () => {
    const uauth = createEnvironment();

    const result = uauth.validateAuthUrl(
        "https://localhost:6110/auth/login",
        "https://localhost:6110"
    );

    assert.equal(
        result,
        "https://localhost:6110/auth/login"
    );
});

test("validateAuthUrl rejects different origin", () => {
    const uauth = createEnvironment();

    assert.throws(() => {
        uauth.validateAuthUrl(
            "https://evil.example.com/auth/login",
            "https://localhost:6110"
        );
    }, /trusted UAuthHub origin/);
});

test("validateAuthUrl rejects javascript protocol", () => {
    const uauth = createEnvironment();

    assert.throws(() => {
        uauth.validateAuthUrl(
            "javascript:alert(1)"
        );
    }, /protocol/);
});

test("validateAuthUrl rejects embedded credentials", () => {
    const uauth = createEnvironment();

    assert.throws(() => {
        uauth.validateAuthUrl(
            "https://user:password@localhost:6110/auth",
            "https://localhost:6110"
        );
    }, /credentials/);
});

test("resourceRequest rejects different API origin", async () => {
    const uauth = createEnvironment();

    await assert.rejects(
        uauth.resourceRequest({
            url: "https://evil.example.com/api/products",
            baseAddress: "https://localhost:6120",
            method: "GET"
        }),
        /configured API origin/
    );
});

test("resourceRequest accepts configured API origin", async () => {
    let capturedUrl;

    const uauth = createEnvironment(async (url) => {
        capturedUrl = url;

        return {
            ok: true,
            status: 200,
            headers: {
                get: () => "application/json"
            },
            text: async () => "[]"
        };
    });

    const result = await uauth.resourceRequest({
        url: "/api/products",
        baseAddress: "https://localhost:6120",
        method: "GET"
    });

    assert.equal(
        capturedUrl,
        "https://localhost:6120/api/products"
    );

    assert.equal(result.ok, true);
    assert.equal(result.status, 200);
    assert.equal(result.body, "[]");
});

test("resourceRequest rejects unsupported HTTP methods", async () => {
    const uauth = createEnvironment();

    await assert.rejects(
        uauth.resourceRequest({
            url: "/api/products",
            baseAddress: "https://localhost:6120",
            method: "TRACE"
        }),
        /Unsupported Resource API method/
    );
});

test("resourceRequest rejects GET with body", async () => {
    const uauth = createEnvironment();

    await assert.rejects(
        uauth.resourceRequest({
            url: "/api/products",
            baseAddress: "https://localhost:6120",
            method: "GET",
            payload: { id: 1 }
        }),
        /GET requests cannot contain a body/
    );
});

test("resourceRequest uses include for cross-origin Auto credentials", async () => {
    let capturedCredentials;

    const uauth = createEnvironment(async (_, options) => {
        capturedCredentials = options.credentials;

        return {
            ok: true,
            status: 204,
            headers: {
                get: () => null
            },
            text: async () => ""
        };
    });

    await uauth.resourceRequest({
        url: "/api/products",
        baseAddress: "https://localhost:6120",
        method: "GET",
        credentials: "Auto"
    });

    assert.equal(capturedCredentials, "include");
});

test("resourceRequest uses same-origin for same-origin Auto credentials", async () => {
    let capturedCredentials;

    const uauth = createEnvironment(async (_, options) => {
        capturedCredentials = options.credentials;

        return {
            ok: true,
            status: 204,
            headers: {
                get: () => null
            },
            text: async () => ""
        };
    });

    await uauth.resourceRequest({
        url: "/api/products",
        baseAddress: "https://localhost:6130",
        method: "GET",
        credentials: "Auto"
    });

    assert.equal(capturedCredentials, "same-origin");
});

test("resourceRequest returns status zero on network error", async () => {
    const uauth = createEnvironment(async () => {
        throw new TypeError("Failed to fetch");
    });

    const result = await uauth.resourceRequest({
        url: "/api/products",
        baseAddress: "https://localhost:6120",
        method: "GET"
    });

    assert.equal(result.ok, false);
    assert.equal(result.status, 0);
});

test("resourceRequest removes request after completion", async () => {
    const uauth = createEnvironment();

    await uauth.resourceRequest({
        url: "/api/products",
        baseAddress: "https://localhost:6120",
        method: "GET",
        requestId: "request-1"
    });

    assert.equal(
        uauth.resourceRequests.has("request-1"),
        false
    );
});

test("resourceRequest rejects duplicate active request IDs", async () => {
    let releaseFetch;

    const pendingFetch = new Promise(resolve => {
        releaseFetch = resolve;
    });

    const uauth = createEnvironment(() => pendingFetch);

    const options = {
        url: "/api/products",
        baseAddress: "https://localhost:6120",
        method: "GET",
        requestId: "duplicate-id"
    };

    const first = uauth.resourceRequest(options);

    await assert.rejects(
        uauth.resourceRequest(options),
        /Duplicate Resource request ID/
    );

    releaseFetch({
        ok: true,
        status: 204,
        headers: {
            get: () => null
        },
        text: async () => ""
    });

    await first;
});

test("cancelResourceRequest aborts active request", async () => {
    let capturedSignal;

    const uauth = createEnvironment((_, options) => {
        capturedSignal = options.signal;

        return new Promise((_, reject) => {
            options.signal.addEventListener("abort", () => {
                const error = new Error("Aborted");
                error.name = "AbortError";
                reject(error);
            });
        });
    });

    const request = uauth.resourceRequest({
        url: "/api/products",
        baseAddress: "https://localhost:6120",
        method: "GET",
        requestId: "cancel-1"
    });

    assert.equal(
        uauth.cancelResourceRequest("cancel-1"),
        true
    );

    await assert.rejects(request, /Aborted/);

    assert.equal(capturedSignal.aborted, true);

    assert.equal(
        uauth.resourceRequests.has("cancel-1"),
        false
    );
});


test("postJson sends antiforgery token to trusted cross-origin UAuthHub", async () => {
    const calls = [];

    const uauth = createEnvironment(async (url, options) => {
        calls.push({ url, options });

        if (url === "https://localhost:6110/auth/antiforgery/token") {
            return {
                ok: true,
                status: 200,
                json: async () => ({
                    requestToken: "csrf-test-token",
                    headerName: "X-CSRF-TOKEN",
                    formFieldName: "__RequestVerificationToken"
                })
            };
        }

        return {
            ok: true,
            status: 200,
            headers: { get: () => null },
            text: async () => JSON.stringify({ isSuccess: true })
        };
    });

    uauth.antiforgery.configure({
        tokenUrl: "https://localhost:6110/auth/antiforgery/token",
        trustedOrigin: "https://localhost:6110"
    });

    const result = await uauth.postJson({
        url: "https://localhost:6110/auth/try-login",
        payload: { username: "test" },
        clientProfile: "BlazorWasm",
        trustedOrigin: "https://localhost:6110"
    });

    assert.equal(result.status, 200);
    assert.equal(calls.length, 2);

    assert.equal(
        calls[0].url,
        "https://localhost:6110/auth/antiforgery/token"
    );
    assert.equal(calls[0].options.method, "GET");
    assert.equal(calls[0].options.credentials, "include");

    assert.equal(
        calls[1].url,
        "https://localhost:6110/auth/try-login"
    );
    assert.equal(calls[1].options.method, "POST");
    assert.equal(calls[1].options.credentials, "include");
    assert.equal(
        calls[1].options.headers["X-CSRF-TOKEN"],
        "csrf-test-token"
    );
});


test("postJson fails closed when antiforgery is not configured", async () => {
    let fetchCalls = 0;

    const uauth = createEnvironment(async () => {
        fetchCalls++;
        throw new Error("Fetch must not be called");
    });

    await assert.rejects(
        uauth.postJson({
            url: authUrl("/auth/try-login"),
            payload: { username: "test" },
            clientProfile: "BlazorServer",
            trustedOrigin: HUB_ORIGIN
        }),
        /Authentication URL is required|antiforgery is not configured/i
    );

    assert.equal(fetchCalls, 0);
});

test("429 try-and-commit returns throttle metadata without committing or reading an auth-result body", async () => {
    let calls = 0;
    const uauth = createEnvironment(csrfAwareFetch(async () => {
        calls++;
        return { ok: false, status: 429, headers: { get: name => name === "Retry-After" ? "90" : null }, text: async () => "not-json" };
    }));

    configureAntiforgery(uauth);

    const result = await uauth.tryAndCommit({
        tryUrl: authUrl("/auth/try-login"),
        commitUrl: authUrl("/auth/login"),
        data: {},
        clientProfile: "BlazorServer",
        trustedOrigin: HUB_ORIGIN
    });
    assert.equal(result.status, 429);
    assert.equal(result.errorCode, "rate_limited");
    assert.equal(result.retryAfter, "90");
    assert.equal(result.isSuccess, false);
    assert.equal(calls, 1);
});

test("logout 429 is observable and never navigates or changes stored tokens", async () => {
    let calls = 0;

    const uauth = createEnvironment(csrfAwareFetch(async (_url, request) => {
        calls++;

        assert.equal(request.redirect, "error");
        assert.equal(
            request.headers["X-UAuth-Logout-Mode"],
            "observe"
        );

        return {
            ok: false,
            status: 429,
            redirected: false,
            headers: {
                get: name => name === "Retry-After" ? "15" : null
            },
            text: async () => ""
        };
    }), () => assert.fail("Rejected logout must not navigate"));

    configureAntiforgery(uauth);

    uauth.storage.set("local", "access", "still-valid-access");
    uauth.storage.set("session", "refresh", "still-valid-refresh");

    const result = await uauth.post({
        url: authUrl("/auth/logout"),
        mode: "logout",
        clientProfile: "BlazorServer",
        trustedOrigin: HUB_ORIGIN
    });

    assert.equal(result.status, 429);
    assert.equal(result.retryAfter, "15");
    assert.equal(uauth.storage.get("local", "access"), "still-valid-access");
    assert.equal(uauth.storage.get("session", "refresh"), "still-valid-refresh");
    assert.equal(calls, 1);
});

test("resource 429 preserves Retry-After without automatic retry", async () => {
    let calls = 0;
    const uauth = createEnvironment(async () => {
        calls++;
        return { ok: false, status: 429, headers: { get: name => name === "Retry-After" ? "Sun, 01 Nov 2026 12:01:00 GMT" : null }, text: async () => "" };
    });
    const result = await uauth.resourceRequest({ url: "/api/data", method: "GET" });
    assert.equal(result.status, 429);
    assert.equal(result.retryAfter, "Sun, 01 Nov 2026 12:01:00 GMT");
    assert.equal(calls, 1);
});

test("refresh 429 leaves stored credentials untouched and sends only once", async () => {
    let calls = 0;
    const uauth = createEnvironment(csrfAwareFetch(async () => {
        calls++;
        return { ok: false, status: 429, headers: { get: () => null }, text: async () => "" };
    }));

    configureAntiforgery(uauth);

    uauth.storage.set("local", "access", "valid-access");
    uauth.storage.set("session", "refresh", "valid-refresh");
    const result = await uauth.post({ url: authUrl("/auth/refresh"), mode: "fetch", clientProfile: "BlazorWasm", trustedOrigin: HUB_ORIGIN });
    assert.equal(result.status, 429);
    assert.equal(uauth.storage.get("local", "access"), "valid-access");
    assert.equal(uauth.storage.get("session", "refresh"), "valid-refresh");
    assert.equal(calls, 1);
});

test("successful observed logout navigates after confirmed success without replaying POST", async () => {
    let calls = 0;
    let navigation = null;

    const uauth = createEnvironment(csrfAwareFetch(async (_url, request) => {
        calls++;

        assert.equal(request.redirect, "error");
        assert.equal(
            request.headers["X-UAuth-Logout-Mode"],
            "observe"
        );

        return {
            ok: true,
            status: 200,
            redirected: false,
            headers: { get: () => null },
            text: async () => JSON.stringify({
                isSuccess: true,
                redirectUrl: "https://localhost:6130/login"
            })
        };
    }), url => navigation = url);

    configureAntiforgery(uauth);

    const result = await uauth.post({
        url: authUrl("/auth/logout"),
        mode: "logout",
        clientProfile: "BlazorWasm",
        trustedOrigin: HUB_ORIGIN
    });

    assert.equal(result.status, 200);
    assert.equal(navigation, "https://localhost:6130/login");
    assert.equal(calls, 1);
});

test("observed logout rejects unsafe redirect protocols", async () => {
    let navigation = null;

    const uauth = createEnvironment(csrfAwareFetch(async () => ({
        ok: true,
        status: 200,
        redirected: false,
        headers: { get: () => null },
        text: async () => JSON.stringify({
            isSuccess: true,
            redirectUrl: "javascript:alert(1)"
        })
    })), url => navigation = url);

    configureAntiforgery(uauth);

    await assert.rejects(
        uauth.post({
            url: authUrl("/auth/logout"),
            mode: "logout",
            clientProfile: "BlazorWasm",
            trustedOrigin: HUB_ORIGIN
        }),
        /Invalid logout redirect protocol/
    );

    assert.equal(navigation, null);
});

test("authenticated logout uses the same Device ID for CSRF token and POST", async () => {
    const deviceId = "test-device";
    let tokenDeviceId = null;
    let logoutDeviceId = null;

    const uauth = createEnvironment(async (url, options) => {
        if (url === authUrl("/auth/antiforgery/token")) {
            tokenDeviceId = options.headers?.["X-UDID"] ?? null;

            return {
                ok: true,
                status: 200,
                json: async () => ({
                    requestToken: "csrf-test-token",
                    headerName: "X-UAuth-CSRF",
                    formFieldName: "__RequestVerificationToken"
                })
            };
        }

        assert.equal(url, authUrl("/auth/logout"));

        logoutDeviceId = options.headers?.["X-UDID"] ?? null;

        return {
            ok: true,
            status: 200,
            redirected: false,
            headers: { get: () => null },
            text: async () => JSON.stringify({
                isSuccess: true,
                redirectUrl: null
            })
        };
    });

    configureAntiforgery(uauth);

    const result = await uauth.post({
        url: authUrl("/auth/logout"),
        mode: "logout-observe",
        clientProfile: "BlazorServer",
        trustedOrigin: HUB_ORIGIN
    });

    assert.equal(result.status, 200);
    assert.equal(logoutDeviceId, deviceId);
    assert.equal(tokenDeviceId, deviceId);
});

class TestForm {
    constructor(fields = []) {
        this.elements = fields.map(field => ({
            ...field,
            disabled: field.disabled ?? false
        }));
        this.action = "";
        this.submitCount = 0;
    }

    appendChild(input) {
        this.elements.push(input);
    }

    querySelector(selector) {
        const match = selector.match(/^input\[name='(.+)'\]$/);
        return match
            ? this.elements.find(x => x.name === match[1]) ?? null
            : null;
    }

    submit() {
        this.submitCount++;
    }
}

class TestFormData {
    constructor(form) {
        this.fields = form.elements.filter(x => !x.disabled && x.name);
    }

    getAll(name) {
        return this.fields
            .filter(x => x.name === name)
            .map(x => x.value);
    }

    get(name) {
        return this.getAll(name)[0] ?? null;
    }
}

function createLoginEnvironment() {
    const uauth = createEnvironment();

// The VM context used by createEnvironment needs these constructors.
// Expose them in createEnvironment's vm.createContext object.
    return uauth;
}

test("submitLoginForm TryOnly reads native credentials without committing", async () => {
    const uauth = createEnvironment();

    const form = new TestForm([
        { name: "Identifier", value: "alice" },
        { name: "Secret", value: "password123" }
    ]);

    let captured = null;

    uauth.postJson = async options => {
        captured = options;
        return {
            status: 200,
            body: { isSuccess: true }
        };
    };

    const result = await uauth.submitLoginForm(form, {
        mode: "TryOnly",
        tryUrl: "/auth/try-login",
        commitUrl: "/auth/login",
        clientProfile: "BlazorServer",
        trustedOrigin: null,
        isPkce: false
    });

    assert.equal(result.isSuccess, true);
    assert.equal(captured.payload.Identifier, "alice");
    assert.equal(captured.payload.Secret, "password123");
    assert.equal(form.submitCount, 0);
});

test("submitLoginForm rejects disabled credential fields", async () => {
    const uauth = createEnvironment();

    const form = new TestForm([
        { name: "Identifier", value: "alice", disabled: true },
        { name: "Secret", value: "password123", disabled: true }
    ]);

    await assert.rejects(
        uauth.submitLoginForm(form, {
            mode: "TryAndCommit",
            tryUrl: "/auth/try-login",
            commitUrl: "/auth/login",
            clientProfile: "BlazorServer",
            trustedOrigin: null,
            isPkce: false
        }),
        /exactly one Identifier and one Secret/
    );

    assert.equal(form.submitCount, 0);
});

test("submitLoginForm TryAndCommit reuses original form", async () => {
    const uauth = createEnvironment();

    const form = new TestForm([
        { name: "Identifier", value: "alice" },
        { name: "Secret", value: "password123" }
    ]);

    uauth.postJson = async () => ({
        status: 200,
        body: {
            isSuccess: true,
            previewReceipt: "receipt-123"
        }
    });

    uauth.antiforgery.appendToForm = async () => { };

    const result = await uauth.submitLoginForm(form, {
        mode: "TryAndCommit",
        tryUrl: "/auth/try-login",
        commitUrl: "/auth/login",
        clientProfile: "BlazorServer",
        trustedOrigin: null,
        isPkce: false
    });

    assert.equal(result.isSuccess, true);
    assert.equal(form.submitCount, 1);
    assert.equal(form.action, "https://localhost:6130/auth/login");

    assert.equal(
        form.elements.find(x => x.name === "PreviewReceipt")?.value,
        "receipt-123"
    );

    assert.equal(
        form.elements.filter(x => x.name === "Secret").length,
        1
    );
});
