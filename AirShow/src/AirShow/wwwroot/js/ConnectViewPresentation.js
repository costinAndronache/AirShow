///<reference path="Definitions/PDFJS.d.ts"/>
var ConnectViewPresentationHelper = (function () {
    function ConnectViewPresentationHelper(pdfURL, canvasId) {
        this.pdfURL = pdfURL;
        this.canvasId = canvasId;
        this.currentDisplayedPage = -1;
        this.canvas = document.getElementById(this.canvasId);
        this.isShowingPointer = false;
        this.radius = 10;
    }
    ConnectViewPresentationHelper.prototype.run = function () {
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
    ConnectViewPresentationHelper.prototype.nextStepAfterLoadingPDF = function () {
        this.displayPage(1);
    };
    ConnectViewPresentationHelper.prototype.drawWithCurrentState = function () {
        var self = this;
        var ctx = self.canvas.getContext('2d');
        this.drawDataUri(function () {
            if (self.isShowingPointer) {
                drawCircleInCanvas(self.pointerCenterX, self.pointerCenterY, self.radius, self.canvas);
            }
        });
    };
    ConnectViewPresentationHelper.prototype.drawDataUri = function (callback) {
        var canvas = this.canvas;
        var image = new Image();
        image.src = this.currentDisplayedDataImage;
        image.addEventListener("load", function () {
            canvas.getContext('2d').drawImage(image, 0, 0);
            callback();
        });
    };
    ConnectViewPresentationHelper.prototype.displayPage = function (index) {
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
    ConnectViewPresentationHelper.prototype.displayNextPage = function () {
        if (this.currentDisplayedPage < this.pdfFile.numPages) {
            this.displayPage(this.currentDisplayedPage + 1);
        }
    };
    ConnectViewPresentationHelper.prototype.displayPreviousPage = function () {
        if (this.currentDisplayedPage > 1) {
            this.displayPage(this.currentDisplayedPage - 1);
        }
    };
    ConnectViewPresentationHelper.prototype.setupControls = function () {
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
    ConnectViewPresentationHelper.prototype.makeCanvasFullScreen = function () {
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
    ConnectViewPresentationHelper.prototype.showPointer = function () {
        this.isShowingPointer = true;
        this.drawWithCurrentState();
    };
    ConnectViewPresentationHelper.prototype.hidePointer = function () {
        this.isShowingPointer = false;
        this.drawWithCurrentState();
    };
    ConnectViewPresentationHelper.prototype.changePointerXY = function (x, y) {
        this.pointerCenterX = x;
        this.pointerCenterY = y;
        this.drawWithCurrentState();
    };
    ConnectViewPresentationHelper.prototype.increasePointerSize = function () {
        this.radius += 5;
        this.drawWithCurrentState();
    };
    ConnectViewPresentationHelper.prototype.decreasePointerSize = function () {
        this.radius -= 5;
        if (this.radius <= 5) {
            this.radius = 5;
        }
        this.drawWithCurrentState();
    };
    ConnectViewPresentationHelper.prototype.resetPointerSize = function () {
        this.radius = 10;
        this.drawWithCurrentState();
    };
    ConnectViewPresentationHelper.prototype.resetPointerPosition = function () {
        this.pointerCenterX = 0.5;
        this.pointerCenterY = 0.5;
        this.drawWithCurrentState();
    };
    return ConnectViewPresentationHelper;
}());
var ConnectPresentationControllerHelper = (function () {
    function ConnectPresentationControllerHelper(presentationId, presentationHelper) {
        this.presentationId = presentationId;
        this.presentationHelper = presentationHelper;
    }
    ConnectPresentationControllerHelper.prototype.run = function () {
        var xhr = new XMLHttpRequest();
        xhr.open("POST", window.location.origin + "/Control/ConnectViewForPresentation?presentationId=" + this.presentationId);
        var self = this;
        xhr.onreadystatechange = function (ev) {
            if (xhr.readyState === XMLHttpRequest.DONE) {
                var response = JSON.parse(xhr.responseText);
                if (response["error"]) {
                    alert(xhr.responseText);
                }
                if (response["roomToken"]) {
                    alert(response["roomToken"]);
                    self.connectWSWithRoomToken(response["roomToken"]);
                }
            }
        };
        xhr.send();
    };
    ConnectPresentationControllerHelper.prototype.connectWSWithRoomToken = function (token) {
        var self = this;
        this.ws = new WebSocket("ws://" + location.host);
        this.ws.onopen = function (ev) {
            var obj = {};
            obj[RoomTokenKey] = token;
            obj[SideKey] = ViewSide;
            var message = JSON.stringify(obj);
            self.ws.send(message);
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
    ConnectPresentationControllerHelper.prototype.handleMessage = function (message) {
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
            case ActionTypeCode.CloseDueToBeingReplacedAction:
                this.handleDismiss();
                break;
            default: break;
        }
    };
    ConnectPresentationControllerHelper.prototype.handleDismiss = function () {
        this.ws.close();
        alert('You have been dismissed by another party');
    };
    ConnectPresentationControllerHelper.prototype.handleChangePageMessage = function (message) {
        var changePageTypeCode = message[kPageChangeActionTypeKey];
        if (changePageTypeCode == PageChangeActionType.MoveNext) {
            this.presentationHelper.displayNextPage();
        }
        else {
            this.presentationHelper.displayPreviousPage();
        }
    };
    ConnectPresentationControllerHelper.prototype.handleShowPointerMessage = function (message) {
        this.presentationHelper.showPointer();
    };
    ConnectPresentationControllerHelper.prototype.handleHidePointerMessage = function (message) {
        this.presentationHelper.hidePointer();
    };
    ConnectPresentationControllerHelper.prototype.handleChangeXYMessage = function (message) {
        var x = message[kPointerCenterXKey];
        var y = message[kPointerCenterYKey];
        this.presentationHelper.changePointerXY(x, y);
    };
    return ConnectPresentationControllerHelper;
}());
var ConnectActivationHelper = (function () {
    function ConnectActivationHelper(presentationHelper) {
        this.presentationHelper = presentationHelper;
    }
    ConnectActivationHelper.prototype.run = function () {
        var self = this;
        var activateButton = document.getElementById("activateButton");
        activateButton.onclick = function (ev) {
            self.controllerHelper = new ConnectPresentationControllerHelper(window["activationRequestString"], self.presentationHelper);
            activateButton.hidden = true;
            self.controllerHelper.run();
        };
        activateButton.hidden = true;
        this.presentationHelper.callbackWhenPdfLoaded = function () {
            activateButton.hidden = false;
        };
    };
    return ConnectActivationHelper;
}());
window.addEventListener("load", function () {
    var greeter = new ConnectViewPresentationHelper(window["presentationURL"], "pdfHost");
    var activationHelper = new ConnectActivationHelper(greeter);
    greeter.run();
    activationHelper.run();
    window["activationHelper"] = activationHelper;
});
//# sourceMappingURL=ConnectViewPresentation.js.map