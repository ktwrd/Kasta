/**
* Author and copyright: Stefan Haack (https://shaack.com)
* Repository: https://github.com/shaack/bootstrap-auto-dark-mode
* License: MIT, see file 'LICENSE'
*/

; (function () {
    const htmlElement = document.querySelector("html");
    if (htmlElement.getAttribute("data-bs-theme") === 'auto') {
        function updateTheme() {
            htmlElement.setAttribute("data-bs-theme",
                globalThis.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light")
        }

        globalThis.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', updateTheme)
        updateTheme()
    }
})()