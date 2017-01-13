var PublicPresentationsHelper = (function () {
    function PublicPresentationsHelper() {
    }
    PublicPresentationsHelper.prototype.run = function () {
        var self = this;
        var buttons = document.getElementsByClassName("addToMyAccountButton");
        if (buttons) {
            for (var i = 0; i < buttons.length; i++) {
                var aButton = buttons[i];
                aButton.onclick = function (ev) {
                    var presentationId = aButton.getAttribute("data-presentationId");
                    self.requestAddToMyAccount(presentationId, function () {
                        aButton.hidden = true;
                    });
                };
            }
        }
    };
    PublicPresentationsHelper.prototype.requestAddToMyAccount = function (presentationId, callbackIfDone) {
        var xhr = new XMLHttpRequest();
        xhr.open("POST", window.location.origin + "/Home/AddToMyPresentations?presentationId=" + presentationId);
        xhr.onreadystatechange = function (ev) {
            if (xhr.readyState === XMLHttpRequest.DONE) {
                if (xhr.status === 200) {
                    callbackIfDone();
                }
                else {
                    alert(xhr.responseText);
                }
            }
        };
        xhr.send();
    };
    return PublicPresentationsHelper;
}());
window.addEventListener("load", function () {
    var helper = new PublicPresentationsHelper();
    helper.run();
});
//# sourceMappingURL=PublicPresentations.js.map