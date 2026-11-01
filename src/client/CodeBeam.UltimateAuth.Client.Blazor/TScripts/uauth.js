window.uauth = window.uauth || {};

window.uauth.storage = {
    set: function (scope, key, value) {
        const storage = scope === "local"
            ? window.localStorage
            : window.sessionStorage;

        storage.setItem(key, value);
    },

    get: function (scope, key) {
        const storage = scope === "local"
            ? window.localStorage
            : window.sessionStorage;

        return storage.getItem(key);
    },

    remove: function (scope, key) {
        const storage = scope === "local"
            ? window.localStorage
            : window.sessionStorage;

        storage.removeItem(key);
    },

    exists: function (scope, key) {
        const storage = scope === "local"
            ? window.localStorage
            : window.sessionStorage;

        return storage.getItem(key) !== null;
    }
};

window.uauth.submitForm = function (form) {
    if (!form)
        return;

    if (!window.uauth.deviceId) {
        throw new Error("UAuth deviceId is not initialized.");
    }

    let udid = form.querySelector("input[name='__uauth_device']");
    if (!udid) {
        udid = document.createElement("input");
        udid.type = "hidden";
        udid.name = "__uauth_device";
        form.appendChild(udid);
    }
    udid.value = window.uauth.deviceId;

    form.submit();
};

window.uauth.tryAndCommit = async function (options) {
    const {
        tryUrl,
        commitUrl,
        data,
        clientProfile,
        trustedOrigin
    } = options;

    const validatedTryUrl = window.uauth.validateAuthUrl(
        tryUrl,
        trustedOrigin
    );

    const validatedCommitUrl = window.uauth.validateAuthUrl(
        commitUrl,
        trustedOrigin
    );

    const deviceId = window.uauth.deviceId;

    if (!deviceId) {
        throw new Error("UAuth deviceId is not initialized.");
    }

    const tryResponse = await window.uauth.postJson({
        url: validatedTryUrl,
        payload: data,
        clientProfile,
        trustedOrigin
    });

    if (!tryResponse || !Number.isInteger(tryResponse.status)) {
        throw new Error("Invalid UAuth transport response.");
    }

    const status = tryResponse.status;
    if (status === 429) {
        return { isSuccess: false, status: 429, errorCode: "rate_limited", retryAfter: tryResponse.retryAfter ?? null };
    }
    const isHttpSuccess = status >= 200 && status < 300;
    const isClientError = status >= 400 && status < 500;

    if (!isHttpSuccess && !isClientError) {
        throw new Error(`Unexpected UAuth try response: HTTP ${status}.`);
    }

    const result = tryResponse.body;

    if (!result ||
        typeof result !== "object" ||
        Array.isArray(result) ||
        typeof result.isSuccess !== "boolean") {
        throw new Error("Invalid UAuth try response body.");
    }

    if (isClientError && result.isSuccess) {
        throw new Error("Successful authentication result returned with HTTP error.");
    }

    const normalized = {
        isSuccess: result.isSuccess,
        reason: result.reason ?? null,
        remainingAttempts: result.remainingAttempts ?? null,
        lockoutUntilUtc: result.lockoutUntilUtc ?? null,
        requiresMfa: result.requiresMfa ?? false,
        retryWithNewPkce: result.retryWithNewPkce ?? false,
        previewReceipt: result.previewReceipt ?? null
    };

    if (normalized.isSuccess) {
        const form = document.createElement("form");
        form.method = "POST";
        form.action = validatedCommitUrl;

        for (const key of Object.keys(data)) {
            const input = document.createElement("input");
            input.type = "hidden";
            input.name = key;
            input.value = data[key] ?? "";
            form.appendChild(input);
        }

        if (normalized.previewReceipt) {
            const receipt = document.createElement("input");
            receipt.type = "hidden";
            receipt.name = "PreviewReceipt";
            receipt.value = normalized.previewReceipt;
            form.appendChild(receipt);
        }

        const cp = document.createElement("input");
        cp.type = "hidden";
        cp.name = "__uauth_client_profile";
        cp.value = clientProfile ?? "";
        form.appendChild(cp);

        const udid = document.createElement("input");
        udid.type = "hidden";
        udid.name = "__uauth_device";
        udid.value = deviceId;
        form.appendChild(udid);

        document.body.appendChild(form);
        form.submit();
    }

    return normalized;
};

window.uauth.post = async function (options) {
    const {
        url,
        mode,
        data,
        clientProfile,
        trustedOrigin
    } = options;

    const validatedUrl = window.uauth.validateAuthUrl(
        url,
        trustedOrigin
    );

    if (mode === "navigate") {
        const form = document.createElement("form");
        form.method = "POST";
        form.action = validatedUrl;

        const cp = document.createElement("input");
        cp.type = "hidden";
        cp.name = "__uauth_client_profile";
        cp.value = clientProfile ?? "";
        form.appendChild(cp);

        const udid = document.createElement("input");
        udid.type = "hidden";
        udid.name = "__uauth_device";
        udid.value = window.uauth.deviceId;
        form.appendChild(udid);

        if (data) {
            for (const key in data) {
                const input = document.createElement("input");
                input.type = "hidden";
                input.name = key;
                input.value = data[key];
                form.appendChild(input);
            }
        }

        document.body.appendChild(form);
        form.submit();
        return null;
    }

    return window.uauth.request({
        url: validatedUrl,
        method: "POST",
        payload: data,
        clientProfile,
        contentType: "form",
        credentials: "include",
        allowCrossOriginCredentials: true,
        navigateOnSuccess: mode === "logout",
        headers: mode === "logout" ? { "X-UAuth-Logout-Mode": "observe" } : {}
    });
};

window.uauth.postJson = function (options) {
    const url = window.uauth.validateAuthUrl(
        options.url,
        options.trustedOrigin
    );

    return window.uauth.request({
        url,
        method: "POST",
        payload: options.payload,
        clientProfile: options.clientProfile,
        contentType: "json",
        credentials: "include",
        allowCrossOriginCredentials: true
    });
};

window.uauth.request = async function (options) {
    const {
        followRedirects = false,
        navigateOnSuccess = false,
        url,
        method = "GET",
        payload = null,
        clientProfile = "",
        contentType = "json",
        credentials = "same-origin",
        headers: customHeaders = {}
    } = options;

    if (!url) {
        throw new Error("Request URL is required.");
    }

    const requestUrl = new URL(url, document.baseURI);

    if (!["http:", "https:"].includes(requestUrl.protocol)) {
        throw new Error("Unsupported request URL protocol.");
    }

    const requestMethod = method.toUpperCase();

    if (!["GET", "POST", "PUT", "PATCH", "DELETE"].includes(requestMethod)) {
        throw new Error(`Unsupported HTTP method: ${requestMethod}`);
    }

    if (!["same-origin", "include", "omit"].includes(credentials)) {
        throw new Error("Invalid credentials policy.");
    }

    if (credentials === "include" &&
        requestUrl.origin !== window.location.origin &&
        !options.allowCrossOriginCredentials) {
        throw new Error("Cross-origin credentials require explicit authorization.");
    }

    if (!window.uauth.deviceId) {
        throw new Error("UAuth deviceId is not initialized.");
    }

    const headers = {
        ...customHeaders,
        "X-UDID": window.uauth.deviceId,
        "X-UAuth-ClientProfile": clientProfile,
        "X-Requested-With": "UAuth"
    };

    let body = null;

    if (payload !== null && payload !== undefined) {
        if (requestMethod === "GET") {
            throw new Error("GET requests cannot contain a body.");
        }

        if (contentType === "json") {
            headers["Content-Type"] = "application/json";
            body = JSON.stringify(payload);
        }
        else if (contentType === "form") {
            headers["Content-Type"] = "application/x-www-form-urlencoded";

            body = new URLSearchParams();

            for (const [key, value] of Object.entries(payload)) {
                body.append(key, value ?? "");
            }
        }
        else {
            throw new Error("Unsupported content type.");
        }
    }

    let response;

    try {
        response = await fetch(requestUrl.href, {
            method: requestMethod,
            credentials,
            redirect: followRedirects ? "follow" : "error",
            headers,
            body
        });
    }
    catch (error) {
        if (error instanceof TypeError) {
            return {
                ok: false,
                status: 0,
                refreshOutcome: null,
                body: null
            };
        }

        throw error;
    }

    let responseBody = null;

    if (response.status !== 204 && response.status !== 205) {
        const text = await response.text();

        if (text) {
            try {
                responseBody = JSON.parse(text);
            }
            catch {
                responseBody = text;
            }
        }
    }

    // Only the observed logout transport follows a successful server redirect.
    // A rejected POST never navigates, clears storage, or causes a second POST.
    if (navigateOnSuccess && response.ok) {
        const redirectUrl = responseBody?.redirectUrl;

        if (typeof redirectUrl === "string" && redirectUrl.length > 0) {
            const target = new URL(redirectUrl, window.location.origin);

            if (!["http:", "https:"].includes(target.protocol)) {
                throw new Error("Invalid logout redirect protocol.");
            }

            window.location.assign(target.href);
        }
    }

    return {
        retryAfter: response.headers.get("Retry-After"),
        ok: response.ok,
        status: response.status,
        refreshOutcome: response.headers.get("X-UAuth-Refresh"),
        body: responseBody
    };
};

window.uauth.resourceRequest = async function (options) {
    const {
        url,
        baseAddress,
        method = "GET",
        payload = null,
        clientProfile = "",
        credentials = "Auto",
        requestId
    } = options;

    if (!url || typeof url !== "string") {
        throw new Error("Resource request URL is required.");
    }

    const baseUrl = baseAddress
        ? new URL(baseAddress)
        : new URL(window.location.origin + "/");

    if (!["http:", "https:"].includes(baseUrl.protocol)) {
        throw new Error("Invalid Resource API protocol.");
    }

    if (baseUrl.username || baseUrl.password ||
        baseUrl.search || baseUrl.hash) {
        throw new Error("Invalid Resource API base address.");
    }

    const requestUrl = new URL(url, baseUrl);

    if (requestUrl.origin !== baseUrl.origin) {
        throw new Error("Resource request target differs from configured API origin.");
    }

    if (requestUrl.username || requestUrl.password) {
        throw new Error("Resource URL cannot contain credentials.");
    }

    if (!["http:", "https:"].includes(requestUrl.protocol)) {
        throw new Error("Unsupported Resource API protocol.");
    }

    const requestMethod = method.toUpperCase();

    if (!["GET", "POST", "PUT", "PATCH", "DELETE"].includes(requestMethod)) {
        throw new Error("Unsupported Resource API method.");
    }

    if ((requestMethod === "GET") && payload !== null) {
        throw new Error("GET requests cannot contain a body.");
    }

    let resolvedCredentials;

    switch (credentials.toLowerCase()) {
        case "auto":
            resolvedCredentials =
                requestUrl.origin === window.location.origin
                    ? "same-origin"
                    : "include";
            break;

        case "sameorigin":
            resolvedCredentials = "same-origin";
            break;

        case "include":
            resolvedCredentials = "include";
            break;

        case "omit":
            resolvedCredentials = "omit";
            break;

        default:
            throw new Error("Invalid Resource credentials mode.");
    }

    if (resolvedCredentials === "include" &&
        requestUrl.protocol !== "https:" &&
        !["localhost", "127.0.0.1", "[::1]"].includes(requestUrl.hostname)) {
        throw new Error(
            "Cross-origin credential requests require HTTPS.");
    }

    if (!window.uauth.deviceId) {
        throw new Error("UAuth deviceId is not initialized.");
    }

    const headers = {
        "X-UDID": window.uauth.deviceId,
        "X-UAuth-ClientProfile": clientProfile,
        "X-Requested-With": "UAuth"
    };

    let body = null;

    if (payload !== null && payload !== undefined) {
        headers["Content-Type"] = "application/json";
        body = JSON.stringify(payload);
    }

    let response;

    const controller = new AbortController();

    if (requestId) {
        if (window.uauth.resourceRequests.has(requestId)) {
            throw new Error("Duplicate Resource request ID.");
        }

        window.uauth.resourceRequests.set(requestId, controller);
    }

    try {
        const response = await fetch(requestUrl.href, {
            method: requestMethod,
            credentials: resolvedCredentials,
            redirect: "error",
            headers,
            body,
            signal: controller.signal
        });

        let responseBody = null;

        if (response.status !== 204 && response.status !== 205) {
            const text = await response.text();

            if (text.length > 0) {
                responseBody = text;
            }
        }

        return {
            retryAfter: response.headers.get("Retry-After"),
            ok: response.ok,
            status: response.status,
            body: responseBody,
            contentType: response.headers.get("Content-Type")
        };
    }
    catch (error) {
        if (error?.name === "AbortError") {
            throw error;
        }

        if (error instanceof TypeError) {
            return {
                ok: false,
                status: 0,
                body: null,
                contentType: null
            };
        }

        throw error;
    }
    finally {
        if (requestId) {
            window.uauth.resourceRequests.delete(requestId);
        }
    }
};

window.uauth.resourceRequests = new Map();

window.uauth.cancelResourceRequest = function (requestId) {
    const controller = window.uauth.resourceRequests.get(requestId);

    if (!controller) {
        return false;
    }

    controller.abort();
    return true;
};

window.uauth.validateAuthUrl = function (url, trustedOrigin = null) {
    if (!url || typeof url !== "string") {
        throw new Error("Authentication URL is required.");
    }

    const origin = trustedOrigin
        ? new URL(trustedOrigin, window.location.origin)
        : new URL(window.location.origin);

    const target = new URL(url, document.baseURI);

    if (!["http:", "https:"].includes(origin.protocol)) {
        throw new Error("Unsupported trusted origin protocol.");
    }

    if (!["http:", "https:"].includes(target.protocol)) {
        throw new Error("Unsupported authentication URL protocol.");
    }

    if (origin.username || origin.password) {
        throw new Error("Trusted origin cannot contain credentials.");
    }

    if (target.username || target.password) {
        throw new Error("Authentication URL cannot contain credentials.");
    }

    if (target.origin !== origin.origin) {
        throw new Error("Authentication request target differs from the trusted UAuthHub origin.");
    }

    return target.href;
};

window.uauth.setDeviceId = function (value) {
    window.uauth.deviceId = value;
};

window.uauth.getDeviceInfo = function () {
    return {
        userAgent: navigator.userAgent,
        platform: navigator.platform,
        language: navigator.language
    };
};
