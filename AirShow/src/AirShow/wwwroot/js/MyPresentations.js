///<reference path="Definitions/jquery.d.ts"/>
var MyPresentationsHelper = (function () {
    function MyPresentationsHelper() {
    }
    MyPresentationsHelper.prototype.run = function () {
        this.setupDeleteButtons();
    };
    MyPresentationsHelper.prototype.setupDeleteButtons = function () {
        var self = this;
        var deleteButtons = document.getElementsByClassName("deletePresentationButton");
        for (var i = 0; i < deleteButtons.length; i++) {
            var button = deleteButtons[i];
            (function (btn) {
                btn.onclick = function (ev) {
                    var dataname = btn.getAttribute("data-name");
                    self.requestDeletePresentation(dataname);
                };
            })(button);
        }
    };
    MyPresentationsHelper.prototype.requestDeletePresentation = function (name) {
        var xhr = new XMLHttpRequest();
        xhr.open("DELETE", window.location.origin + "/Presentations/DeletePresentation?name=" + name);
        xhr.onreadystatechange = function (ev) {
            if (xhr.readyState === XMLHttpRequest.DONE) {
                if (xhr.status === 200) {
                    window.location.reload();
                }
                else {
                    alert('An error has occured. Please try refreshing the page');
                }
            }
        };
        xhr.send();
    };
    return MyPresentationsHelper;
}());
window.addEventListener("load", function () {
    var helper = new MyPresentationsHelper();
    helper.run();
});
//# sourceMappingURL=MyPresentations.js.map