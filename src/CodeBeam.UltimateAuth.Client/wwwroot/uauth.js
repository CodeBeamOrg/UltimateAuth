window.uauth = {
    submitForm: function (form) {
        if (form) {
            form.submit();
        }
    }
};

window.uauth = {
    post: function (action, data) {
        const form = document.createElement("form");
        form.method = "POST";
        form.action = action;

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
    },

    refresh: async function (action) {
        const response = await fetch(action, {
            method: "POST",
            credentials: "include"
        });

        return {
            ok: response.ok,
            status: response.status,
            refreshOutcome: response.headers.get("X-UAuth-Refresh")
        };
    }
};
