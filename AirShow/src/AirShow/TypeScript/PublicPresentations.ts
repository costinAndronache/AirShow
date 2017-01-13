
class PublicPresentationsHelper {

    run() {

        var self = this;

        var buttons = document.getElementsByClassName("addToMyAccountButton");
        if (buttons) {
            for (var i = 0; i < buttons.length; i++) {
                var aButton = buttons[i] as HTMLButtonElement;
                aButton.onclick = function (ev: Event) {
                    var presentationId = aButton.getAttribute("data-presentationId");
                    self.requestAddToMyAccount(presentationId, function () {
                        aButton.hidden = true;
                    });
                }
            }
        }

    }


    private requestAddToMyAccount(presentationId: string, callbackIfDone: () => void) {

        var xhr = new XMLHttpRequest();
        xhr.open("POST", window.location.origin + "/Home/AddToMyPresentations?presentationId=" + presentationId);
        
        xhr.onreadystatechange = function (ev: ProgressEvent) {
            if (xhr.readyState === XMLHttpRequest.DONE) {
                if (xhr.status === 200) {
                    callbackIfDone();
                }
                else {
                    alert(xhr.responseText);
                }
            }
        }
        xhr.send();

    }
}

window.addEventListener("load", function () {
    var helper = new PublicPresentationsHelper();
    helper.run();
});