// Renders the album map with Leaflet + OpenStreetMap. Runs only on pages that
// contain the #map element (the home page); markers come from the generated
// albums/markers.json and link through to their album.
//
// Markers are grouped with Leaflet.markercluster so that several trips near (or
// on top of) each other collapse into a single count badge instead of stacking
// invisibly. Zooming in — or clicking the badge — separates them, and trips at
// the exact same coordinates fan out (spiderfy) so each one is individually
// hoverable and clickable. Hovering a pin shows a tooltip with the trip name,
// date and photo count so you can confirm before clicking through.

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

        const cluster = L.markerClusterGroup({
            // The hover ring over a cluster's covered area is noisy for a photo
            // map; the count badge alone communicates "several trips here".
            showCoverageOnHover: false,
        });
        const points = [];

        markers.forEach((marker) => {
            const position = [marker.Lat, marker.Long];

            L.marker(position)
                .bindTooltip(tooltipFor(marker), { direction: "top", offset: [0, -12] })
                .on("click", () => {
                    window.location.href = "album/" + marker.Slug;
                })
                .addTo(cluster);

            points.push(position);
        });

        map.addLayer(cluster);
        map.fitBounds(points, { padding: [20, 20], maxZoom: 12 });
    }

    // Builds the hover tooltip as a DOM node (not an HTML string) so an album
    // name is inserted as text and can never inject markup into the page.
    function tooltipFor(marker) {
        const tip = document.createElement("div");
        tip.className = "map-tip";

        const name = document.createElement("span");
        name.className = "map-tip__name";
        name.textContent = marker.Name || marker.Slug;
        tip.appendChild(name);

        const meta = [];
        if (marker.Date) {
            meta.push(marker.Date);
        }
        if (typeof marker.Photos === "number") {
            meta.push(marker.Photos + (marker.Photos === 1 ? " photo" : " photos"));
        }

        if (meta.length > 0) {
            const detail = document.createElement("span");
            detail.className = "map-tip__meta";
            detail.textContent = meta.join(" · ");
            tip.appendChild(detail);
        }

        return tip;
    }
})();
