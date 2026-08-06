// Initialize and add the map

var map;

function initMap() {
    map = new google.maps.Map(document.getElementById("map"), {
      zoom: 10
    });

    fetch('albums/markers.json')
      .then(function(response){return response.json()})
      .then(plotMarkers);
  }

  function plotMarkers(jsonBlob)
  {
    markers = [];
    bounds = new google.maps.LatLngBounds();

    jsonBlob.forEach(function(marker) {
      var position = new google.maps.LatLng(marker.Lat, marker.Long);

      var actualMarker = new google.maps.Marker({
          position: position,
          map: map,
          animation: google.maps.Animation.DROP
        });

        actualMarker.addListener("click", () => {
        window.location.href = "album/" + marker.Slug
          });

      markers.push(marker);
    
      bounds.extend(position);
    });

    map.fitBounds(bounds);
  }