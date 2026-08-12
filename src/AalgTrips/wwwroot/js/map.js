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

    // A castle trip's pin: a small round CSS marker (styled in site.css). Using a
    // divIcon keeps it a local asset with no extra image download.
    const castleIcon = L.divIcon({
        className: "castle-pin",
        html: "<span class=\"castle-pin__dot\"></span>",
        iconSize: [20, 20],
        iconAnchor: [10, 10],
        tooltipAnchor: [0, -10],
    });

    const map = L.map(mapElement);

    L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors",
    }).addTo(map);

    // Trips near (or on top of) each other collapse into a single count badge.
    // The hover ring over a cluster's covered area is noisy for a photo map; the
    // count badge alone communicates "several trips here".
    const cluster = L.markerClusterGroup({ showCoverageOnHover: false });
    map.addLayer(cluster);

    let allMarkers = [];
    let activeFilter = null;

    // The home-page filters (filters.js) broadcast the active filter; re-plot the
    // pins so the map always matches the filtered trip list. A null detail clears
    // the filter and shows every pin again.
    document.addEventListener("trips:filter", (event) => {
        activeFilter = event.detail;
        render();
    });

    // The marker file's URL is provided by the server (the photo store): a
    // root-relative /albums/markers.json for local disk, or a CDN/blob URL when
    // content is stored in Azure Blob. Fall back to the local path if absent.
    const markersUrl = mapElement.dataset.markersUrl || "/albums/markers.json";

    fetch(markersUrl)
        .then((response) => response.json())
        .then((markers) => {
            allMarkers = Array.isArray(markers) ? markers : [];
            render();
        })
        .catch(() => map.setView([20, 0], 2));

    function render() {
        const shown = allMarkers.filter((marker) => matchesFilter(marker, activeFilter));

        cluster.clearLayers();

        const points = [];

        shown.forEach((marker) => {
            const position = [marker.Lat, marker.Long];

            // Castle trips get a distinct-colour pin (a locally-styled divIcon, so
            // no CDN asset); every other trip keeps Leaflet's default marker.
            const options = marker.Castle ? { icon: castleIcon } : undefined;

            L.marker(position, options)
                .bindTooltip(tooltipFor(marker), { direction: "top", offset: [0, -12] })
                .on("click", () => {
                    window.location.href = "album/" + marker.Slug;
                })
                .addTo(cluster);

            points.push(position);
        });

        if (points.length > 0) {
            map.fitBounds(points, { padding: [20, 20], maxZoom: 12 });
        } else {
            // No matching trips: show the whole world rather than leaving Leaflet
            // without a view (which would throw on interaction).
            map.setView([20, 0], 2);
        }
    }

    function matchesFilter(marker, filter) {
        if (!filter) {
            return true;
        }

        if (filter.castleOnly && !marker.Castle) {
            return false;
        }

        if (filter.people && filter.people.length > 0) {
            const on = marker.People || [];

            // Match the card filter: every selected person must be on the trip (AND),
            // and "exact" additionally requires no other people (exact set).
            if (!filter.people.every((person) => on.includes(person))) {
                return false;
            }

            if (filter.exact && on.length !== filter.people.length) {
                return false;
            }
        }

        return true;
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
