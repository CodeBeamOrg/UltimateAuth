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
})) {
    const window = {
        location: {
            origin: "https://localhost:6130"
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
