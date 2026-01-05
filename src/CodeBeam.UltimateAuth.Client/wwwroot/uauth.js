window.uauth = {
    submitForm: function (form) {
        if (form) {
            form.submit();
        }
    }
};

window.uauth = {
    post: async function (options) {
        const {
            url,
            mode,
            data,
            expectJson,
            clientProfile
        } = options;

        if (mode === "navigate") {
            const form = document.createElement("form");
            form.method = "POST";
            form.action = url;

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

        const response = await fetch(url, {
            method: "POST",
            credentials: "include",
            headers: {
                "X-UAuth-ClientProfile": clientProfile
            }
        });

        let body = null;
        if (expectJson) {
            try { body = await response.json(); } catch { }
        }

        return {
            ok: response.ok,
            status: response.status,
            refreshOutcome: response.headers.get("X-UAuth-Refresh"),
            body: body
        };
    }
};
