(function() {
    var lineColorPalette = ['#e6194b', '#3cb44b', '#ffe119', '#4363d8', '#f58231', '#911eb4', '#46f0f0', '#f032e6', '#bcf60c', '#fabebe', '#008080', '#e6beff', '#9a6324', '#fffac8', '#800000', '#aaffc3', '#808000', '#ffd8b1', '#000075', '#808080', '#ffffff', '#000000'];

    function initMapFrame(data) {
        var mapObj = L.map('mapFrame');

        /*
        L.tileLayer("https://maps.wikimedia.org/osm-intl/{z}/{x}/{y}.png", {
            attribution: '<a href="https://wikimediafoundation.org/wiki/Maps_Terms_of_Use">Mapy Wikimedia</a> | Mapová data © <a href="http://openstreetmap.org/copyright">přispěvatelé OpenStreetMap</a>',
            maxZoom: 19
        }).addTo(mapObj);
        */
        L.tileLayer("https://tile.thunderforest.com/transport/{z}/{x}/{y}{r}.png?apikey=222b90f9396e4a3498d9292cc89be080", {
            attribution: 'Mapy © <a href="http://www.thunderforest.com/">Thunderforest</a> | Mapová data © <a href="http://openstreetmap.org/copyright">přispěvatelé OpenStreetMap</a>',
            maxZoom: 18
        }).addTo(mapObj);

        var trainLines = data.lines;
        var allLinePoints = [];
        for (var i = 0; i < trainLines.length; ++i) {
            var trainLine = trainLines[i];
            L.polyline(trainLine, {color: lineColorPalette[i % lineColorPalette.length]}).addTo(mapObj);
            for (var j = 0; j < trainLine.length; ++j) {
                allLinePoints.push(trainLine[j]);
            }
        }

        var points = data.points;
        for (i = 0; i < points.length; ++i) {
            L.marker(points[i].coords, {title: points[i].title}).addTo(mapObj);
        }

        mapObj.fitBounds(L.latLngBounds(allLinePoints));
    }

    function initGeoLocationButton(buttonContainerId, urlTemplate, resultContainerId) {
        if (!"geolocation" in navigator) return;

        var $button = $('<a href="#" class="btn btn-default">');
        $button.append($('<span class="glyphicon glyphicon-screenshot" aria-label="Geolokace"></span>'));
        $button.click(function() {
            navigator.geolocation.getCurrentPosition(function(position) {
                $.ajax({
                    url: urlTemplate.replace("$lat", position.coords.latitude.toString()).replace("$lon", position.coords.longitude.toString()),
                    dataType: "html",
                    error: function() {
                        $(resultContainerId).html('<div class="alert alert-danger" role="alert">Nepodařilo se zjistit nejbližší místa</div>');
                    },
                    success: function(data) {
                        $(resultContainerId).html(data);
                    }
                });
            }, function(error) {
                $(resultContainerId).html('<div class="alert alert-danger" role="alert">Nepodařilo se zjistit vaši polohu</div>');
            });
        });
        $(buttonContainerId).append($button);
    }
    
    window['kdyPojedeVlak'] = {
        initMapFrame: initMapFrame,
        initGeoLocationButton: initGeoLocationButton
    };
})();
