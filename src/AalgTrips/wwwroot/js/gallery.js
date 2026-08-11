// Turns the album thumbnail grid into a PhotoSwipe lightbox. Progressive
// enhancement: each thumbnail's <a> still points at the per-photo page (used
// with no JS, by crawlers, for shared deep links, and for admin management),
// while this opens the lightbox on a plain click using the full-size image
// declared in the data-pswp-* attributes. Runs only where #gallery exists.
//
// It also adds deep-linking and a share button: the open photo is reflected in
// the URL fragment (#photo=<name>), a link with that fragment reopens the
// lightbox on that photo, and a toolbar button copies that shareable link.

import PhotoSwipeLightbox from "/lib/photoswipe/photoswipe-lightbox.esm.min.js";

const gallery = document.getElementById("gallery");

if (gallery) {
    const lightbox = new PhotoSwipeLightbox({
        gallery: "#gallery",
        children: "a[data-pswp-src]",
        pswpModule: () => import("/lib/photoswipe/photoswipe.esm.min.js"),
    });

    // The lightbox slides come from these anchors, in this order, so the same
    // list maps a PhotoSwipe index to the photo's shareable name (data-text).
    const slides = Array.from(gallery.querySelectorAll("a[data-pswp-src]"));

    const photoName = (index) => (slides[index] ? slides[index].dataset.text : null);

    const shareUrl = (name) =>
        location.origin + location.pathname + location.search + "#photo=" + encodeURIComponent(name);

    // The anchor href is the per-photo page, not the image, so take the large
    // image PhotoSwipe displays from data-pswp-src instead of the href. Width and
    // height are read from data-pswp-width/height by PhotoSwipe's defaults.
    lightbox.addFilter("domItemData", (itemData, element, linkEl) => {
        const source = linkEl && linkEl.dataset.pswpSrc;

        if (source) {
            itemData.src = source;
        }

        return itemData;
    });

    // Reflect the currently-open photo in the URL fragment so it can be shared or
    // bookmarked; clear it again when the lightbox closes.
    lightbox.on("change", () => {
        const name = photoName(lightbox.pswp.currIndex);

        if (name) {
            history.replaceState(null, "", "#photo=" + encodeURIComponent(name));
        }
    });

    lightbox.on("close", () => {
        history.replaceState(null, "", location.pathname + location.search);
    });

    // Add a "copy link" button to the lightbox toolbar. PhotoSwipe 5 has no
    // built-in share, so register a custom button that copies the deep link to
    // the open photo.
    lightbox.on("uiRegister", () => {
        lightbox.pswp.ui.registerElement({
            name: "share-link",
            className: "pswp__button--share",
            // Place it in the top toolbar next to zoom/close; without this it is
            // appended to the overlay root and ends up hidden behind the photo.
            appendTo: "bar",
            order: 8,
            isButton: true,
            title: "Copy link to this photo",
            html:
                // Fill with the primary icon colour (white) like PhotoSwipe's own
                // icons; "currentColor" here resolves to the dark secondary colour
                // .pswp__icn sets, which left the icon looking washed out.
                '<svg class="pswp__icn" viewBox="0 0 24 24" aria-hidden="true">' +
                '<path fill="var(--pswp-icon-color)" d="M8.5 12a3 3 0 0 1 3-3H15V7h-3.5a5 5 0 0 0 0 10H15v-2h-3.5a3 3 0 0 1-3-3zm3-1h7v2h-7v-2zM20.5 7H17v2h3.5a3 3 0 0 1 0 6H17v2h3.5a5 5 0 0 0 0-10z"/>' +
                "</svg>",
            onClick: (event, el, pswp) => {
                const name = photoName(pswp.currIndex);

                if (!name) {
                    return;
                }

                const url = shareUrl(name);
                gallery.dataset.pswpShareUrl = url;
                copyToClipboard(url);
                el.setAttribute("title", "Link copied");
            },
        });
    });

    lightbox.init();

    // Open the lightbox straight to a photo when the page is loaded with a
    // #photo=<name> fragment (a shared deep link), and again if the fragment
    // later changes (e.g. navigating to a shared link on the already-open album).
    // history.replaceState (used above) does not fire hashchange, so reflecting
    // the open slide in the URL will not retrigger this.
    openFromHash();
    window.addEventListener("hashchange", openFromHash);

    // Signal that the lightbox is bound and clicks will open it (rather than
    // following the anchor to the per-photo page). Handy for tests and for any
    // styling that should only apply once the gallery is interactive.
    gallery.dataset.pswpReady = "true";

    function openFromHash() {
        // Ignore if the lightbox is already open (guards against reopening when a
        // hashchange fires while it is showing).
        if (lightbox.pswp) {
            return;
        }

        const match = location.hash.match(/^#photo=(.*)$/);

        if (!match) {
            return;
        }

        const name = decodeURIComponent(match[1]);
        const index = slides.findIndex((slide) => slide.dataset.text === name);

        if (index >= 0) {
            lightbox.loadAndOpen(index);
        }
    }

    function copyToClipboard(text) {
        try {
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(text).catch(() => fallbackCopy(text));
            } else {
                fallbackCopy(text);
            }
        } catch {
            fallbackCopy(text);
        }
    }

    function fallbackCopy(text) {
        const field = document.createElement("textarea");
        field.value = text;
        field.setAttribute("readonly", "");
        field.style.position = "absolute";
        field.style.left = "-9999px";
        document.body.appendChild(field);
        field.select();

        try {
            document.execCommand("copy");
        } catch {
            /* best effort */
        }

        document.body.removeChild(field);
    }
}
