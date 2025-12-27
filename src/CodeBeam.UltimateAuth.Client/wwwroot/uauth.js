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
    }
};

