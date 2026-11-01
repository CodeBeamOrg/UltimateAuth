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
}), onNavigate = () => {}) {
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
        baseURI: "https://localhost:6130/"
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
        TypeError
    });

    vm.runInContext(script, context);

    window.uauth.setDeviceId("test-device");

    return window.uauth;
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

test("429 try-and-commit returns throttle metadata without committing or reading an auth-result body", async () => {
    let calls = 0;
    const uauth = createEnvironment(async () => {
        calls++;
        return { ok: false, status: 429, headers: { get: name => name === "Retry-After" ? "90" : null }, text: async () => "not-json" };
    });
    const result = await uauth.tryAndCommit({ tryUrl: "/auth/try-login", commitUrl: "/auth/login", data: {}, clientProfile: "BlazorServer" });
    assert.equal(result.status, 429);
    assert.equal(result.errorCode, "rate_limited");
    assert.equal(result.retryAfter, "90");
    assert.equal(result.isSuccess, false);
    assert.equal(calls, 1);
});

test("logout 429 is observable and never navigates or changes stored tokens", async () => {
    let calls = 0;

    const uauth = createEnvironment(async (_url, request) => {
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
    }, () => assert.fail("Rejected logout must not navigate"));

    uauth.storage.set("local", "access", "still-valid-access");
    uauth.storage.set("session", "refresh", "still-valid-refresh");

    const result = await uauth.post({
        url: "/auth/logout",
        mode: "logout",
        clientProfile: "BlazorServer"
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
    const uauth = createEnvironment(async () => {
        calls++;
        return { ok: false, status: 429, headers: { get: () => null }, text: async () => "" };
    });
    uauth.storage.set("local", "access", "valid-access");
    uauth.storage.set("session", "refresh", "valid-refresh");
    const result = await uauth.post({ url: "/auth/refresh", mode: "fetch", clientProfile: "BlazorWasm" });
    assert.equal(result.status, 429);
    assert.equal(uauth.storage.get("local", "access"), "valid-access");
    assert.equal(uauth.storage.get("session", "refresh"), "valid-refresh");
    assert.equal(calls, 1);
 });

test("successful observed logout navigates after confirmed success without replaying POST", async () => {
    let calls = 0;
    let navigation = null;

    const uauth = createEnvironment(async (_url, request) => {
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
    }, url => navigation = url);

    const result = await uauth.post({
        url: "/auth/logout",
        mode: "logout",
        clientProfile: "BlazorWasm"
    });

    assert.equal(result.status, 200);
    assert.equal(navigation, "https://localhost:6130/login");
    assert.equal(calls, 1);
});

test("observed logout rejects unsafe redirect protocols", async () => {
    let navigation = null;

    const uauth = createEnvironment(async () => ({
        ok: true,
        status: 200,
        redirected: false,
        headers: { get: () => null },
        text: async () => JSON.stringify({
            isSuccess: true,
            redirectUrl: "javascript:alert(1)"
        })
    }), url => navigation = url);

    await assert.rejects(
        uauth.post({
            url: "/auth/logout",
            mode: "logout",
            clientProfile: "BlazorWasm"
        }),
        /Invalid logout redirect protocol/
    );

    assert.equal(navigation, null);
});
