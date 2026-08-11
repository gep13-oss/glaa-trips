(() => {

    // Fade images in as they load
    const pics = document.getElementsByTagName("img");

    for (let img of pics) {

        img.onload = (e) => {
            e.target.className = "loaded";
        };

        if (img.complete) {
            setTimeout((elm) => {
                elm.className = "loaded";
            }, 200, img);
        }
    }

    // Keyboard navigation: left/right follow the page's prev/next links. Wire this
    // up ONLY on pages without the album gallery — i.e. the standalone per-photo
    // page, where prev/next are the neighbouring photos. On the album page those
    // links point at adjacent albums (jumping trips is not what an arrow press
    // should do), and its PhotoSwipe lightbox handles left/right photo navigation
    // itself, so binding here would hijack the arrows out of the lightbox.
    if (!document.getElementById("gallery")) {
        const keyMap = {
            37: document.querySelector("a[rel=prev]"), // left
            39: document.querySelector("a[rel=next]") // right
        };

        window.addEventListener("keyup", (e) => {
            if (e.altKey || e.shiftKey || e.ctrlKey) {
                return;
            }

            const link = keyMap[e.keyCode];

            if (link) {
                location.href = link.href;
            }
        }, false);
    }

})();