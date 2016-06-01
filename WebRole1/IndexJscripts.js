$(function () {

    $("#stat").hide();
    $("#searchResults").hide();

    $("#playerSearch").keyup(function () {
            getArticles();
    })

    function getArticles() {
        $.ajax({
            crossDomain: true,
            url: "http://ec2-52-33-124-190.us-west-2.compute.amazonaws.com/nbaQuery.php",
            data: { 'name': $("#playerSearch").val() },
            dataType: "jsonp",
            success: onDataReceived
        });

        $.ajax({
            type: "POST",
            url: "admin.asmx/GetRelevantArticles",
            data: "{ searchQuery : '" + $("#playerSearch").val().removeAll("'", " ") + "'}",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (data) {
                $("#searchResults").show();
                $("#searchResults").html("");
                var arr = data.d.substring(2, data.d.length - 2).split("},{");
                if (arr.length != 1) {
                    for (var i = 0; i < arr.length; i++) {
                        var arrData = arr[i].split(",");
                        var titleData = arrData[1].removeAll('\\u0027', "'").removeAll('\\u0026#39;', "'").slice(1, -1).split('":"');
                        var title = $("<h3 class='aTitle'></h3>").text(titleData[1]);
                        var urlData = arrData[0].slice(1, -1).split('":"');
                        var url = $("<a href=" + urlData[1] + "></a").text(urlData[1]);
                        var dateData = arrData[2].slice(1, -1).split('":"')
                        var date = $("<h4 class='aDate'></h4>").text(dateData[1]);
                        var result = $("<div class='articleFound'></div>").append(title, url, date);
                        $("#searchResults").append(result);
                    }
                } else {
                    var none = $("<h4></h4>").text("No Articles Found");
                    var result = $("<div class='articleFound'></div>").append(none);
                    $("#searchResults").append(result);
                }
            }
        });
    }

    function onDataReceived(data) {
        var arr = JSON.parse(JSON.stringify(data));
        if (arr.length != 0) {
            $("#playerPic").attr("src", "http://i.cdn.turner.com/nba/nba/.element/img/2.0/sect/statscube/players/large/" + $("#playerSearch").val().replace(" ", "_") + ".png");
            $("#teamPic").attr("src", "http://stats.nba.com/media/img/teams/logos/" + arr[0]["Team"] + "_logo.svg");
            $("#stat").show();
            $("#pName").html(arr[0]["Name"]);
            $("#tName").html("Team : " + arr[0]["Team"]);
            $("#gPlayed").html("Games Played : " + arr[0]["Games Played"]);
            $("#minutes").html("Minutes : " + arr[0]["Minutes"]);
            $("#assists").html("Assists : " + arr[0]["Assists"]);
            $("#turnovers").html("Turnovers : " + arr[0]["Turnovers"]);
            $("#steals").html("Steals : " + arr[0]["Steals"]);
            $("#blocks").html("Blocks : " + arr[0]["Blocks"]);
            $("#fgm").html(arr[0]["Field Goals Made"]);
            $("#fga").html(arr[0]["Field Goals Attempted"]);
            $("#fgp").html(arr[0]["Field Goals Percentage"] + '%');
            $("#3pm").html(arr[0]["3 Points Made"]);
            $("#3pa").html(arr[0]["3 Points Attempted"]);
            $("#3pp").html(arr[0]["3 Points Percentage"] + '%');
        } else {
            $("#stat").hide();
        }
    }

    $("#playerSearch").keyup(function () {
        $.ajax({
            type: "POST",
            url: "admin.asmx/searchTrie",
            data: "{ title :" + JSON.stringify($("#playerSearch").val()) + "}",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                var line = msg.d.substring(1, msg.d.length - 1);
                var data = line.split(",");
                $("#suggestions").empty();
                for (var i = 0; i < data.length; i++) {
                    var suggestion = document.createElement("li");
                    suggestion.innerHTML = data[i].substring(1, data[i].length - 1).removeAll('_', " ");
                    suggestion.onclick = function () {
                        $("#playerSearch").val($(this).html());
                        getArticles();
                    };
                    $("#suggestions").append(suggestion);
                }
                if (!$("#playerSearch").val()) {
                    $("#suggestions").empty();
                }
            }
        });
    });
});

String.prototype.removeAll = function (target, replacement) {
    return this.split(target).join(replacement);
};

