$(document).ready(function () {

    //start signalR connection
    $.connection.hub.logging = true;
    $.connection.hub
       .start();

    //place holders for elements clicked on to start video conversion
    var clickedVidImg;
    var clickedVidImgLink;

    //fetch songs associated with the playlist clicked on
    $('.playlistBtn').on('click', function () {
        var userID = $(this).attr('data-userid');
        var token = $(this).attr('data-token');
        var playlistID = $(this).attr('data-playlistid');

        $.connection.spotifySongsHub.server.getPlaylistSongs(playlistID, token, userID);

    });

    //start the search on youtube for the song selected
    $('#jsonstuff').on('click', ".searchSong", function () {
        var songName = $(this).attr('data-songname');
        var artistName = $(this).attr('data-artistname');

        $.connection.spotifySongsHub.server.getSearchedSongs(songName, artistName);
    });

    //start video conversion for song selected
    $('#vidList').on('click', '.btn', function () {
        var vidID = $(this).attr('data-vidid');
        var vidUrl = $(this).attr('data-vidurl');

        //get the Img url of the video to download and change it to the loading gif
        clickedVidImg = $(this).parent().prev().children().children();
        clickedVidImgLink = clickedVidImg.attr('src');
        clickedVidImg.attr('src', "../content/images/converting.gif");

        //disable download buttons
        $('.btn').attr("disabled", "disabled");
        $.connection.myHub.server.announce(vidUrl);
    });

    //load the playlists songs into a table to be displayed
    $.connection.spotifySongsHub.client.getPlaylistSongs = function (tracks) {
        $('#jsonstuff').empty();
        $('#jsonstuff').append("<table><tr><th>Name</th><th>Artist</th></tr>");
        $.map(tracks, function (track) {
            $('#jsonstuff').append("<tr><td>" + track.name + "</td><td>" + track.artist + "</td><td>" +
                "<input type=\"button\" class=\"searchSong\" value=\"Search\" data-songname=\"" + track.name + "\" data-artistname=\"" + track.artist + "\" />" +
                "</td></tr>");
        });
        $('#jsonstuff').append("</table>");
    };

    //get the youtube search results and load them to be displayed
    $.connection.spotifySongsHub.client.getSearchedSongs = function (vids) {
        $('#vidList').empty();

        var html = "<div class=\"row\">";
        vids.forEach(function (vid) {
            if (((vids.indexOf(vid)) % 3) == 0 && vids.indexOf(vid) != 0) {
                html += "</div><div class=\"row\">";
            }

            html += "</div><div id=\"dataListItem\" class=\"col-md-4\"><dl class=\"dl-horizontal\">" +
                "<dt><a href=\"https://www.youtube.com/watch?v=" + vid.url.replace("/watch?v=", "") + "\" target=\"_blank\"><img id=\"" + vid.id + "\" src=" + vid.imgLink + "width=\"100\" height=\"75\" /></a></dt>" +
                "<dd style=\"padding-top:15px; padding-left:10px\"><input type=\"button\" value=\"Download\" class=\"btn btn-danger\" data-vidid=\"" + vid.id + "\" data-vidurl=\"" + vid.url.replace("/watch?v=", "") + "\" /></dd>" +
                "<dl>" +
                "<dt>Tittle: </dt>" +
                "<dd>" + vid.title + "</dd>" +
                "<dt>Uploader: </dt>" +
                "<dd>" + vid.uploader + "</dd>" +
                "<dt>Duration: </dt>" +
                "<dd>" + vid.duration + "</dd>" +
                "</dl></dl></div></div>";
        });
        html += "</div>";
        $('#vidList').append(html);
    };

    //start return the video that was converted 
    $.connection.myHub.client.announce = function (song) {
        var subbtn = document.getElementById("dwnLdBtn");
        subbtn.href = "/Home/Downlaod/" + song;
        subbtn.click();
        //change the load image back the the youtube vide image
        clickedVidImg.attr('src', clickedVidImgLink);
        //enable the download buttons
        $('.btn').removeAttr("disabled");
    };

    $.connection.spotifySongsHub.client.msg = function (msg) {
        alert(msg);
    }

    $.connection.hub.error(function (error) {
        console.log('SignalR error: ' + error)
    });
});

