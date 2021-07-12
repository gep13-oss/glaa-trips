// Initialize and add the map
function initMap() {
    // The location of Uluru
    const uluru = { lat: 57.4151, lng: -1.8325 };
    // The map, centered at Uluru
    const map = new google.maps.Map(document.getElementById("map"), {
      zoom: 10,
      center: uluru,
    });
    // The marker, positioned at Uluru
    const marker = new google.maps.Marker({
      position: uluru,
      title: "Slains Castle",
      label: "Bob"
    });
  
    marker.addListener("click", () => {
      window.location.href = "http://127.0.0.1:5500/slains";
        });
  
    marker.setMap(map);
  }