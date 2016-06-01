window.onload = function () {
    google.charts.load('current', { packages: ['corechart', 'line'] });
    google.charts.setOnLoadCallback(drawAxisTickColors);

    function refreshStats() {
        $.ajax({
            type: "POST",
            url: "admin.asmx/GetAllStats",
            data: "",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                data = msg;
                var arr = data.d.substring(1, data.d.length - 1).split(",");
                for (var i = 0; i < arr.length; i++) {
                    arr[i] = arr[i].slice(1, -1);
                }
                console.log(arr);

                // Populate Page with Stats
                if (arr[0] == "Active") {
                    $("#cState").css('color', 'green');
                } else if (arr[0] == "Idle") {
                    $("#cState").css('color', '#ffdb00');
                } else {
                    $("#cState").css('color', 'red');
                }
                $("#cState").html(arr[0]);
                if (!isNaN(arr[1] )) {
                    $("#CPU").html(parseFloat(arr[1]).toFixed(2));
                    $("#cpuFilled").css("width", arr[1] + "%");
                    drawAxisTickColors(arr[16], "cpuDiv", "CPU %");
                } else {
                    $("#CPU").html("Retrieving");
                    $("#cpuFilled").css("width", "0%");
                    drawAxisTickColors("0 0|", "cpuDiv", "CPU %");
                }
                if (!isNaN(arr[2] )) {
                    $("#ramU").html(parseFloat(arr[2]).toFixed(2));
                    $("#ramFilled").css("width", (arr[2] / 1024) * 100 + "%");
                    drawAxisTickColors(arr[16], "ramDiv", "RAM Usage");
                } else {
                    $("#ramU").html("Retrieving");
                    $("#ramFilled").css("width", "0%");
                    drawAxisTickColors("0 0|", "ramDiv", "RAM Usage");
                }
                if (!isNaN(arr[2] )) {
                    $("#ramA").html((1024 - parseInt(arr[2])).toFixed(2));
                    $("#ramAFilled").css("width", ((1024 - arr[2]) / 1024) * 100 + "%");
                    drawAxisTickColors(arr[16], "ramADiv", "RAM Available");
                } else {
                    $("#ramU").html("Retrieving");
                    $("#ramAFilled").css("width", "0%");
                    drawAxisTickColors("0 0|", "ramADiv", "RAM Available");
                }
                $("#urlsCrawled").html(arr[3]);
                $("#queueSize").html(arr[14]);
                $("#tableSize").html(arr[15]);
                $("#lastTitle").html(arr[17]);
                $("#titlesStored").html(arr[18]);
                $("#errorTable").html("Error Links Found : ");
                $("#lastTenTable").html("Last Ten URLs crawled : ");
                for (var i = 13; i >= 4; i--) {
                    if (arr[i] != "No Data") {
                        var url = $("<a href='" + arr[i] + "'></h4>").text(arr[i]);
                    } else {
                        var url = $("<h4></h4>").text(arr[i]);
                    }
                    var link = $("<div class='linkDiv'></div>")
                    link.append(url);
                    $("#lastTenTable").append(link);
                }
                for (var i = arr.length - 1; i >= 19; i--) {
                    var error = arr[i].split("|||");
                    if (error[0] != "No " && error[1] != " Data") {
                        var url = $("<a href='" + error[0] + "'></h4>").text(error[0]);
                        var msg = $("<h4></h4>").text("Message : " + error[1]);
                        var errorDiv = $("<div class='errorDiv'></div>")
                        errorDiv.append(url, msg);
                        $("#errorTable").append(errorDiv);
                    }
                }
            },
            error: function (request, status, error) {
                $("#cState").html("Error");
                $("#cState").css('color', 'red');
            } 
        });
    }
    var timer = setInterval(refreshStats, 5000);
};

// Starts the crawler
$(function () {
    $("#start").click(function () {
        $.ajax({
            type: "POST",
            url: "admin.asmx/StartCrawling",
            data: "",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                $("#buttonMessage").html("Activating Crawler");
                $("#buttonMessage").css("background-color", "#90EE90");
                setTimeout(function () { $("#buttonMessage").html(""); }, 10000);
            }
        });
    });
});

// Stops the crawler
$(function () {
    $("#stop").click(function () {
        $.ajax({
            type: "POST",
            url: "admin.asmx/StopCrawling",
            data: "",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                $("#buttonMessage").html("Stopping Crawler, state will change promptly.");
                $("#buttonMessage").css("background-color", "#ffcccc");
                setTimeout(function () { $("#buttonMessage").html(""); }, 10000);
            }
        });
    });
});

// Clears the crawler
$(function () {
    $("#clear").click(function () {
        $.ajax({
            type: "POST",
            url: "admin.asmx/ClearIndex",
            data: "",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                $("#start").prop("disabled", true);
                $("#start").css("opacity", ".5");
                $("#buttonMessage").html("Clearing crawler, Start Button disabled until ready.");
                setTimeout(function () { $("#buttonMessage").html(""); $("#start").prop("disabled", false); $("#start").css("opacity", "1"); }, 40000);
            }
        });
    });
});

// Submits a query for the article title
$(function () {
    $("#submit").click(function () {
        $.ajax({
            type: "POST",
            url: "admin.asmx/GetPageTitle",
            data: "{ url : '" + $("#desiredURL").val() + "'}",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                data = msg;
                var arr = data.d.substring(1, data.d.length - 1).split(",");
                for (var i = 0; i < arr.length; i++) {
                    arr[i] = arr[i].slice(1, -1);
                }
                console.log(arr);
                $("#pageStats").html("");
                var title = $("<h4></h4>").text("Title : " + arr[0]);
                var date = $("<h4></h4>").text("Publish Date : " + arr[1]);
                var url = $("<a href='" + arr[2] + "' ></a>").text(arr[2]);
                $("#pageStats").append(title, date, url);
            }
        });
    });
});

// Creates a chart of performance data for the last hour
function drawAxisTickColors(values, divName, statName) {
    $("#" + divName).html = "";
    var data = new google.visualization.DataTable();
    data.addColumn('number', 'X');
    data.addColumn('number', statName);

    console.log(values);
    values = values.substring(0, values.length - 1);
    var points = values.split("|");
    for (var i = 0; i < points.length; i++) {
        var cpuOrRam = points[i].split(" ");
        if (divName == "cpuDiv") {
            data.addRows([
                [5 * i, parseInt(cpuOrRam[0])]
            ])
        } else if (divName == "ramDiv") {
            data.addRows([
                [5 * i, parseInt(cpuOrRam[1])]
            ])
        } else {
            data.addRows([
                [5 * i, 1024 - parseInt(cpuOrRam[1])]
            ])
        }
    }

    var options = {
        hAxis: {
            title: 'Time (Last Hour)',
            textStyle: {
                color: '#7e7e7e',
                fontSize: 20,
                fontName: 'Open Sans',
                bold: false,
                italic: false
            },
            titleTextStyle: {
                color: '#7e7e7e',
                fontSize: 16,
                fontName: 'Open Sans',
                bold: false,
                italic: false
            }
        },
        vAxis: {
            title: statName,
            textStyle: {
                color: '#7e7e7e',
                fontSize: 16,
                bold: false,
                italic: false
            },
            titleTextStyle: {
                color: '#7e7e7e',
                fontSize: 16,
                bold: false,
                italic: false
            }
        },
        colors: ['#b20000', '#b20000']
    };
    var chart = new google.visualization.LineChart(document.getElementById(divName));
    chart.draw(data, options);
}
