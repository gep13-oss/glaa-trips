// Turns the album thumbnail grid into a PhotoSwipe lightbox. Progressive
// enhancement: each thumbnail's <a> still points at the per-photo page (used
// with no JS, by crawlers, for shared deep links, and for admin management),
// while this opens the lightbox on a plain click using the full-size image
// declared in the data-pswp-* attributes. Runs only where #gallery exists.

import PhotoSwipeLightbox from "/lib/photoswipe/photoswipe-lightbox.esm.min.js";

const gallery = document.getElementById("gallery");

if (gallery) {
    const lightbox = new PhotoSwipeLightbox({
        gallery: "#gallery",
        children: "a[data-pswp-src]",
        pswpModule: () => import("/lib/photoswipe/photoswipe.esm.min.js"),
    });

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

    lightbox.init();

    // Signal that the lightbox is bound and clicks will open it (rather than
    // following the anchor to the per-photo page). Handy for tests and for any
    // styling that should only apply once the gallery is interactive.
    gallery.dataset.pswpReady = "true";
}
