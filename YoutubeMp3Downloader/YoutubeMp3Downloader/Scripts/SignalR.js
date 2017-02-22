var youtubeImgTemp = "";
var youtubeImgId;

function x(imgID, link) {
    youtubeImgTemp = document.getElementById(imgID).src;
    youtubeImgId = imgID;
    document.getElementById(imgID).src = "../content/images/converting.gif";

    $.connection.hub
        .start()
        .done(function () {
            $.connection.myHub.server.announce(link);
        })
        .fail();
}

$.connection.myHub.client.announce = function (song) {
    var subbtn = document.getElementById("dwnLdBtn");
    subbtn.href = "/Home/Downlaod/" + song;
    subbtn.click();
    document.getElementById(youtubeImgId).src = youtubeImgTemp;
    
};

