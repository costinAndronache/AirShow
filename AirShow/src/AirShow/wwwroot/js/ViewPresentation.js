///<reference path="Definitions/PDFJS.d.ts"/>
var ViewPresentationHelper = (function () {
    function ViewPresentationHelper(pdfURL, canvasId) {
        this.pdfURL = pdfURL;
        this.canvasId = canvasId;
        this.currentDisplayedPage = -1;
        this.canvas = document.getElementById(this.canvasId);
    }
    ViewPresentationHelper.prototype.run = function () {
        var self = this;
        PDFJS.workerSrc = "/lib/pdfjs-dist/build/pdf.worker.min.js";
        PDFJS.getDocument(this.pdfURL).then(function (pdfPromise) {
            self.pdfFile = pdfPromise;
            self.nextStepAfterLoadingPDF();
        });
    };
    ViewPresentationHelper.prototype.nextStepAfterLoadingPDF = function () {
        this.displayPage(1);
        this.setupControls();
    };
    ViewPresentationHelper.prototype.displayPage = function (index) {
        var self = this;
        this.pdfFile.getPage(index).then(function (page) {
            var context = self.canvas.getContext('2d');
            var viewport = page.getViewport(1);
            viewport = page.getViewport(1.0);
            self.canvas.height = viewport.height;
            self.canvas.width = viewport.width;
            var renderContext = {
                canvasContext: context,
                viewport: viewport
            };
            page.render(renderContext);
            self.currentDisplayedPage = index;
        });
    };
    ViewPresentationHelper.prototype.displayNextPage = function () {
        if (this.currentDisplayedPage < this.pdfFile.numPages) {
            this.displayPage(this.currentDisplayedPage + 1);
        }
    };
    ViewPresentationHelper.prototype.displayPreviousPage = function () {
        if (this.currentDisplayedPage > 1) {
            this.displayPage(this.currentDisplayedPage - 1);
        }
    };
    ViewPresentationHelper.prototype.setupControls = function () {
        var fullScreenButton = document.getElementById("fullScreenButton");
        var self = this;
        fullScreenButton.onclick = function (ev) {
            if (document.fullscreenElement) {
                if (document.exitFullscreen) {
                    document.exitFullscreen();
                }
            }
            else {
                if (!self.makeCanvasFullScreen())
                    alert('This browser does not have full-screen capabilities');
            }
        };
        window.addEventListener('keydown', function (ev) {
            if (ev.keyCode == 37) {
                self.displayPreviousPage();
            }
            if (ev.keyCode == 39) {
                self.displayNextPage();
            }
        });
    };
    ViewPresentationHelper.prototype.makeCanvasFullScreen = function () {
        var cv = this.canvas;
        var adjustWidthHeight = function () {
            cv.style.height = "100%";
            cv.style.width = "100%";
        };
        if (cv.requestFullscreen) {
            cv.requestFullscreen();
            adjustWidthHeight();
            return true;
        }
        if (cv.webkitRequestFullScreen) {
            cv.webkitRequestFullScreen();
            adjustWidthHeight();
            return true;
        }
        return false;
    };
    return ViewPresentationHelper;
}());
var PresentationControllerHelper = (function () {
    function PresentationControllerHelper(connectionString, presentationHelper) {
        this.connectionString = connectionString;
        this.presentationHelper = presentationHelper;
    }
    PresentationControllerHelper.prototype.run = function () {
        var self = this;
        this.ws = new WebSocket("ws://" + location.host);
        this.ws.onopen = function (ev) {
            self.ws.send(window["activationRequestString"]);
            alert('Now you can login on your remote device and control this presentation by going to \"My active presentations\". Do not close this page');
        };
        this.ws.onerror = function (ev) {
            alert("There was an error though");
        };
        this.ws.onclose = function (ev) {
            alert("closed " + ev.code);
        };
        this.ws.onmessage = function (ev) {
            var message = JSON.parse(ev.data);
            self.handleMessage(message);
        };
    };
    PresentationControllerHelper.prototype.handleMessage = function (message) {
        var messageCode = message[kActionTypeCodeKey];
        if (messageCode == ActionTypeCode.PageChangeAction) {
            this.handleChangePageMessage(message);
        }
    };
    PresentationControllerHelper.prototype.handleChangePageMessage = function (message) {
        var changePageTypeCode = message[kPageChangeActionTypeKey];
        if (changePageTypeCode == PageChangeActionType.MoveNext) {
            this.presentationHelper.displayNextPage();
        }
        else {
            this.presentationHelper.displayPreviousPage();
        }
    };
    return PresentationControllerHelper;
}());
var ActivationHelper = (function () {
    function ActivationHelper(presentationHelper) {
        this.presentationHelper = presentationHelper;
    }
    ActivationHelper.prototype.run = function () {
        var self = this;
        var activateButton = document.getElementById("activateButton");
        activateButton.onclick = function (ev) {
            self.controllerHelper = new PresentationControllerHelper(window["activationRequestString"], self.presentationHelper);
            activateButton.hidden = true;
            self.controllerHelper.run();
        };
    };
    return ActivationHelper;
}());
window.addEventListener("load", function () {
    var greeter = new ViewPresentationHelper(window["presentationURL"], "pdfHost");
    var activationHelper = new ActivationHelper(greeter);
    greeter.run();
    activationHelper.run();
    window["activationHelper"] = activationHelper;
});
//# sourceMappingURL=ViewPresentation.js.map