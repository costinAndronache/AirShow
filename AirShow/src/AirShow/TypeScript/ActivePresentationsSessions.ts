

window.addEventListener("load", function () {

    var buttons = document.getElementsByClassName("stopButton") as NodeListOf<HTMLButtonElement>;
    
    for (var i = 0; i < buttons.length; i++) {
        var button = buttons[i];

        (function (btn: HTMLButtonElement) {

            var presentationId = btn.getAttribute("data-presentationId");
            var btnParent = document.getElementById(presentationId) as HTMLDivElement;

            btn.onclick = function () {

                var xhr = new XMLHttpRequest();
                xhr.open("DELETE", "/Control/ForceStopSessionForPresentation?presentationId=" + presentationId);
                xhr.onreadystatechange = function () {
                    if (xhr.readyState == XMLHttpRequest.DONE) {
                        if (xhr.status != 200) {
                            var errorJson = JSON.parse(xhr.responseText);
                            alert(errorJson["error"] + "\n");
                        } else {
                            btnParent.parentNode.removeChild(btnParent);
                        }

                    }
                }

                xhr.send();
            }

        })(button);
    }

});