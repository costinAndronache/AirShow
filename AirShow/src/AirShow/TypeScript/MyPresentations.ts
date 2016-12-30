///<reference path="Definitions/jquery.d.ts"/>

class MyPresentationsHelper {

    run() {
        this.setupDeleteButtons();
    }


    private setupDeleteButtons() {
        var self = this;
        var deleteButtons = document.getElementsByClassName("deletePresentationButton");
        for (var i = 0; i < deleteButtons.length; i++) {
            var button = deleteButtons[i] as HTMLButtonElement;
            (function (btn: HTMLButtonElement) {
                btn.onclick = function (ev: Event) {
                    var dataname = btn.getAttribute("data-name");
                    self.requestDeletePresentation(dataname);
                }
            })(button);
        }
    }


    private requestDeletePresentation(name: string) {
        var xhr = new XMLHttpRequest();
        xhr.open("DELETE", window.location.origin + "/Presentations/DeletePresentation?name=" + name);
        xhr.onreadystatechange = function (ev: ProgressEvent) {
            if (xhr.readyState === XMLHttpRequest.DONE) {
                if (xhr.status === 200) {
                    window.location.reload();
                }
                else {
                    alert('An error has occured. Please try refreshing the page');
                }
            }
        }
        xhr.send();
    }
}



window.onload = function (ev: Event) {
    var helper = new MyPresentationsHelper();
    helper.run();
}