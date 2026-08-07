// Renders the album map with Leaflet + OpenStreetMap. Runs only on pages that
// contain the #map element (the home page); markers come from the generated
// albums/markers.json and link through to their album.

(() => {
    const mapElement = document.getElementById("map");

    if (!mapElement) {
        return;
    }

    // Serve Leaflet's default marker images from the locally-hosted copy so the
    // markers render without reaching out to a CDN.
    L.Icon.Default.imagePath = "/lib/leaflet/images/";

    const map = L.map(mapElement);

    L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors",
    }).addTo(map);

    // The marker file's URL is provided by the server (the photo store): a
    // root-relative /albums/markers.json for local disk, or a CDN/blob URL when
    // content is stored in Azure Blob. Fall back to the local path if absent.
    const markersUrl = mapElement.dataset.markersUrl || "/albums/markers.json";

    fetch(markersUrl)
        .then((response) => response.json())
        .then(plotMarkers)
        .catch(() => map.setView([20, 0], 2));

    function plotMarkers(markers) {
        if (!Array.isArray(markers) || markers.length === 0) {
            // No albums with coordinates yet: show the whole world rather than
            // leaving Leaflet without a view (which would throw on interaction).
            map.setView([20, 0], 2);
            return;
        }

        const points = [];

        markers.forEach((marker) => {
            const position = [marker.Lat, marker.Long];

            L.marker(position)
                .addTo(map)
                .on("click", () => {
                    window.location.href = "album/" + marker.Slug;
                });

            points.push(position);
        });

        map.fitBounds(points, { padding: [20, 20], maxZoom: 12 });
    }
})();
