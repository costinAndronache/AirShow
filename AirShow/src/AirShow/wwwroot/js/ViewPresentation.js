///<reference path="Definitions/PDFJS.d.ts"/>
var ViewPresentationHelper = (function () {
    function ViewPresentationHelper(pdfURL, canvasId) {
        this.pdfURL = pdfURL;
        this.canvasId = canvasId;
        this.currentDisplayedPage = -1;
        this.canvas = document.getElementById(this.canvasId);
        this.isShowingPointer = false;
        this.radius = 10;
    }
    ViewPresentationHelper.prototype.run = function () {
        this.setupControls();
        var loadingIndicatorDiv = document.getElementById("loadingIndicatorDiv");
        var topCanvasContainer = document.getElementById("topCanvasContainer");
        topCanvasContainer.hidden = true;
        this.fullScreenButton.hidden = true;
        var self = this;
        PDFJS.workerSrc = "/lib/pdfjs-dist/build/pdf.worker.min.js";
        PDFJS.getDocument(this.pdfURL).then(function (pdfPromise) {
            loadingIndicatorDiv.style.height = "0";
            loadingIndicatorDiv.hidden = true;
            self.fullScreenButton.hidden = false;
            topCanvasContainer.hidden = false;
            self.pdfFile = pdfPromise;
            self.callbackWhenPdfLoaded();
            self.nextStepAfterLoadingPDF();
        });
    };
    ViewPresentationHelper.prototype.nextStepAfterLoadingPDF = function () {
        this.displayPage(1);
    };
    ViewPresentationHelper.prototype.drawWithCurrentState = function () {
        var self = this;
        var ctx = self.canvas.getContext('2d');
        this.drawDataUri(function () {
            if (self.isShowingPointer) {
                drawCircleInCanvas(self.pointerCenterX, self.pointerCenterY, self.radius, self.canvas);
            }
        });
    };
    ViewPresentationHelper.prototype.drawDataUri = function (callback) {
        var canvas = this.canvas;
        var image = new Image();
        image.src = this.currentDisplayedDataImage;
        image.addEventListener("load", function () {
            canvas.getContext('2d').drawImage(image, 0, 0);
            callback();
        });
    };
    ViewPresentationHelper.prototype.displayPage = function (index) {
        var self = this;
        this.pdfFile.getPage(index).then(function (page) {
            var context = self.canvas.getContext('2d');
            var viewport = page.getViewport(1);
            viewport = page.getViewport(1.0);
            var parentDiv = self.canvas.parentElement;
            self.canvas.height = viewport.height;
            self.canvas.width = viewport.width;
            var renderContext = {
                canvasContext: context,
                viewport: viewport
            };
            page.render(renderContext).then(function (page) {
                self.currentDisplayedDataImage = self.canvas.toDataURL();
                self.pointerCenterX = 0.5;
                self.pointerCenterY = 0.5;
                self.drawWithCurrentState();
            });
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
        this.fullScreenButton = document.getElementById("fullScreenButton");
        var self = this;
        this.fullScreenButton.onclick = function (ev) {
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
                return false;
            }
            if (ev.keyCode == 39) {
                self.displayNextPage();
                return false;
            }
        });
    };
    ViewPresentationHelper.prototype.makeCanvasFullScreen = function () {
        var cv = this.canvas;
        var adjustWidthHeight = function () {
            cv.style.height = "100%";
            cv.style.width = "auto";
        };
        var oldHeight = cv.style.height;
        var oldWidth = cv.style.width;
        var whenExitingFullScreen = function () {
            cv.style.width = oldWidth;
            cv.style.height = oldHeight;
        };
        if (cv.requestFullscreen) {
            cv.requestFullscreen();
            adjustWidthHeight();
            return true;
        }
        var self = this;
        var exitHandler = function () {
            if (document.webkitIsFullScreen || document.fullscreenElement || document.mozFullScreen || document.msFullscreenElement) {
            }
            else {
                whenExitingFullScreen();
                document.removeEventListener('webkitfullscreenchange', this, false);
                document.removeEventListener('mozfullscreenchange', this, false);
                document.removeEventListener('fullscreenchange', this, false);
                document.removeEventListener('MSFullscreenChange', this, false);
            }
        };
        if (document.addEventListener) {
            document.addEventListener('webkitfullscreenchange', exitHandler, false);
            document.addEventListener('mozfullscreenchange', exitHandler, false);
            document.addEventListener('fullscreenchange', exitHandler, false);
            document.addEventListener('MSFullscreenChange', exitHandler, false);
        }
        if (cv.webkitRequestFullScreen) {
            cv.webkitRequestFullScreen();
            adjustWidthHeight();
            return true;
        }
        return false;
    };
    ViewPresentationHelper.prototype.showPointer = function () {
        this.isShowingPointer = true;
        this.drawWithCurrentState();
    };
    ViewPresentationHelper.prototype.hidePointer = function () {
        this.isShowingPointer = false;
        this.drawWithCurrentState();
    };
    ViewPresentationHelper.prototype.changePointerXY = function (x, y) {
        this.pointerCenterX = x;
        this.pointerCenterY = y;
        this.drawWithCurrentState();
    };
    ViewPresentationHelper.prototype.increasePointerSize = function () {
        this.radius += 5;
        this.drawWithCurrentState();
    };
    ViewPresentationHelper.prototype.decreasePointerSize = function () {
        this.radius -= 5;
        if (this.radius <= 5) {
            this.radius = 5;
        }
        this.drawWithCurrentState();
    };
    ViewPresentationHelper.prototype.resetPointerSize = function () {
        this.radius = 10;
        this.drawWithCurrentState();
    };
    ViewPresentationHelper.prototype.resetPointerPosition = function () {
        this.pointerCenterX = 0.5;
        this.pointerCenterY = 0.5;
        this.drawWithCurrentState();
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
            jQuery("#modalMessageView").modal("show");
        };
        this.ws.onerror = function (ev) {
        };
        this.ws.onclose = function (ev) {
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
        console.log(message);
        switch (messageCode) {
            case ActionTypeCode.IncreasePointerSizeAction:
                this.presentationHelper.increasePointerSize();
                break;
            case ActionTypeCode.DeceasePointerSizeAction:
                this.presentationHelper.decreasePointerSize();
                break;
            case ActionTypeCode.ResetPointerPositionAction:
                this.presentationHelper.resetPointerPosition();
                break;
            case ActionTypeCode.ChangePointerOriginAction:
                this.handleChangeXYMessage(message);
                break;
            case ActionTypeCode.ShowPointerAction:
                this.presentationHelper.showPointer();
                break;
            case ActionTypeCode.HidePointerAction:
                this.presentationHelper.hidePointer();
                break;
            case ActionTypeCode.ResetPointerSizeAction:
                this.presentationHelper.resetPointerSize();
                break;
            default: break;
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
    PresentationControllerHelper.prototype.handleShowPointerMessage = function (message) {
        this.presentationHelper.showPointer();
    };
    PresentationControllerHelper.prototype.handleHidePointerMessage = function (message) {
        this.presentationHelper.hidePointer();
    };
    PresentationControllerHelper.prototype.handleChangeXYMessage = function (message) {
        var x = message[kPointerCenterXKey];
        var y = message[kPointerCenterYKey];
        this.presentationHelper.changePointerXY(x, y);
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
        activateButton.hidden = true;
        this.presentationHelper.callbackWhenPdfLoaded = function () {
            activateButton.hidden = false;
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