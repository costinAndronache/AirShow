///<reference path="Definitions/PDFJS.d.ts"/>
var ViewPresentationHelper = (function () {
    function ViewPresentationHelper(pdfURL, canvasId) {
        this.pdfURL = pdfURL;
        this.canvasId = canvasId;
        this.currentDisplayedPage = -1;
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
            var canvas = document.getElementById(self.canvasId);
            var context = canvas.getContext('2d');
            var viewport = page.getViewport(1);
            var diff = window.screen.height - 2 * absoluteY(canvas);
            var scale = (diff) / viewport.height;
            console.log(diff + ", " + viewport.height + ", " + scale);
            viewport = page.getViewport(0.7);
            canvas.height = viewport.height;
            canvas.width = viewport.width;
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
        var previousButton = document.getElementById("previousButton");
        var nextButton = document.getElementById("nextButton");
        var self = this;
        previousButton.onclick = function (ev) {
            self.displayPreviousPage();
        };
        nextButton.onclick = function (ev) {
            self.displayNextPage();
        };
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
        };
        this.ws.onerror = function (ev) {
            alert("There was an error though");
        };
        this.ws.onclose = function (ev) {
            alert("Again closed " + ev.code);
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
window.onload = function (ev) {
    var greeter = new ViewPresentationHelper(window["presentationURL"], "pdfHost");
    var activationHelper = new ActivationHelper(greeter);
    greeter.run();
    activationHelper.run();
    window["activationHelper"] = activationHelper;
};
//# sourceMappingURL=ViewPresentation.js.map