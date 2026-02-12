(function () {
    function setText(id, text) {
        const el = document.getElementById(id);
        if (el) el.textContent = text;
    }

    function setHtml(id, html) {
        const el = document.getElementById(id);
        if (el) el.innerHTML = html;
    }

    function show(id) {
        const el = document.getElementById(id);
        if (el) el.style.display = "block";
    }

    function hide(id) {
        const el = document.getElementById(id);
        if (el) el.style.display = "none";
    }

    function backOrHome() {
        if (window.history.length > 1) window.history.back();
        else window.location.href = "/index.html";
    }

    window.ui = { setText, setHtml, show, hide, backOrHome };
})();
