window.uaDocsScrollSpy = (() => {

    let lastId = null;
    let handler = null;

    function start(dotnetRef) {

        stop();

        handler = () => {

            const elements = document.querySelectorAll(".docs-content h2, .docs-content h3");
            if (!elements.length) return;

            const center = window.innerHeight / 2;

            let best = null;
            let minDiff = Number.MAX_SAFE_INTEGER;

            for (const el of elements) {
                const rect = el.getBoundingClientRect();
                const diff = Math.abs(rect.top - center);

                if (rect.top <= center && diff < minDiff) {
                    minDiff = diff;
                    best = el;
                }
            }

            if (!best) return;

            const id = best.id;

            if (id !== lastId) {
                lastId = id;
                history.replaceState(null, "", window.location.pathname + "#" + id);
                dotnetRef.invokeMethodAsync("SetActiveHeading", id);
            }
        };

        document.addEventListener('scroll', handler, true);
        window.addEventListener('resize', handler, true);

        handler();
    }

    function stop() {
        if (handler) {
            document.removeEventListener('scroll', handler, true);
            window.removeEventListener('resize', handler, true);
            handler = null;
        }
    }

    function scrollTo(id) {
        const el = document.getElementById(id);
        if (!el) return;

        const offset = 300;
        const y = window.scrollY + el.getBoundingClientRect().top - offset;

        window.scrollTo({
            top: y,
            behavior: "smooth"
        });
    }

    return {
        start,
        stop,
        scrollTo
    };
})();

window.highlightDocsCode = () => {
    if (window.Prism) {
        Prism.highlightAllUnder(
            document.querySelector(".docs-content") ?? document
        );
    }
};

document.addEventListener("click", async (event) => {
    const button = event.target.closest(".docs-code-copy");

    if (!button) return;

    const wrapper = button.closest(".docs-code-wrapper");
    const code = wrapper?.querySelector("pre code");

    if (!code) return;

    const originalText = button.textContent;

    try {
        await navigator.clipboard.writeText(code.textContent);

        button.textContent = "Copied!";
        button.classList.add("copied");

        setTimeout(() => {
            button.textContent = originalText;
            button.classList.remove("copied");
        }, 2000);
    }
    catch (error) {
        console.error("Failed to copy code:", error);
    }
});
