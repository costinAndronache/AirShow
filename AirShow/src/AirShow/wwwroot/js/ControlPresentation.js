///<reference path="Common.ts"/>
var PointerCanvasController = (function () {
    function PointerCanvasController() {
        this.canvas = document.getElementById("pointerCanvas");
        this.pointerCenterX = 0.5;
        this.pointerCenterY = 0.5;
        this.canvasContainer = document.getElementById("pointerCanvasContainer");
        this.toolsDiv = document.getElementById("pointerControlsContainer");
        this.modalDivContainer = document.getElementById("modalContainer");
        this.resizeCanvasAfterParent();
        this.isShown = false;
        this.radius = 10;
    }
    PointerCanvasController.prototype.expand = function () {
        this.isShown = true;
        jQuery("#modalContainer").modal("show");
        this.callbackOnShow();
    };
    PointerCanvasController.prototype.contract = function () {
        this.isShown = false;
        jQuery("#modalContainer").modal("hide");
        this.callbackOnHide();
    };
    PointerCanvasController.prototype.resizeCanvasAfterParent = function () {
        this.canvas.width = this.canvasContainer.clientWidth;
        this.canvas.height = this.canvasContainer.clientHeight;
    };
    PointerCanvasController.prototype.toggleDisplayOrHide = function () {
        if (this.isShown) {
            this.contract();
        }
        else {
            this.expand();
        }
    };
    PointerCanvasController.prototype.run = function () {
        this.setupControls();
    };
    PointerCanvasController.prototype.setupControls = function () {
        var self = this;
        var canvasParent = this.canvas.parentElement;
        document.body.addEventListener("touchmove", function (ev) {
            if (ev.target == canvasParent) {
                ev.preventDefault();
            }
        }, false);
        window.addEventListener("resize", function (ev) {
            self.resizeCanvasAfterParent();
            self.drawWithCurrentState();
        });
        var redrawWithCoordinates = function (coordX, coordY) {
            self.pointerCenterX = coordX / self.canvas.width;
            self.pointerCenterY = coordY / self.canvas.height;
            self.callbackOnChangeXY(self.pointerCenterX, self.pointerCenterY);
            self.drawWithCurrentState();
        };
        var touchMoveHandler = function (ev) {
            var touch = ev.targetTouches[0];
            var x = touch.clientX - self.canvas.clientLeft;
            var y = touch.clientY - self.canvas.clientTop;
            redrawWithCoordinates(x, y);
        };
        var pointerHandler = function (ev) {
            redrawWithCoordinates(ev.offsetX, ev.offsetY);
        };
        var eventType;
        if (window.navigator.pointerEnabled) {
            eventType = "pointermove";
            canvasParent.addEventListener(eventType, pointerHandler, false);
        }
        else if (window.navigator.msPointerEnabled) {
            eventType = "MSPointerMove";
            canvasParent.addEventListener(eventType, pointerHandler, false);
        }
        else {
            eventType = "touchmove";
            canvasParent.addEventListener(eventType, touchMoveHandler, false);
        }
        canvasParent.addEventListener("mousemove", function (ev) {
            if (ev.buttons == 1) {
                redrawWithCoordinates(ev.offsetX, ev.offsetY);
            }
        });
        jQuery("#modalContainer").on("hidden.bs.modal", function () {
            self.isShown = false;
            jQuery("#modalContainer").modal("hide");
            self.callbackOnHide();
        });
        jQuery("#modalContainer").on("shown.bs.modal", function () {
            self.resizeCanvasAfterParent();
            self.drawWithCurrentState();
        });
        var resetSizeButton = document.getElementById("resetSizeButton");
        var resetPositionButton = document.getElementById("resetPositionButton");
        var increaseSizeButton = document.getElementById("increaseSizeButton");
        var decreaseSizeButton = document.getElementById("decreaseSizeButton");
        var toggleButton = document.getElementById("toggleToolsButton");
        toggleButton.onclick = function () {
            self.toggleDisplayOrHide();
        };
        resetPositionButton.onclick = function () {
            self.resetPosition();
        };
        resetSizeButton.onclick = function () {
            self.radius = 10;
            self.callbackOnResetPointerSize();
            self.drawWithCurrentState();
        };
        increaseSizeButton.onclick = function () {
            self.radius += 5;
            self.callbackOnIncreasePointerSize();
            self.drawWithCurrentState();
        };
        decreaseSizeButton.onclick = function () {
            if (self.radius <= 5) {
                return;
            }
            self.radius -= 5;
            self.callbackOnDecreasePointerSize();
            self.drawWithCurrentState();
        };
    };
    PointerCanvasController.prototype.resetPosition = function () {
        this.pointerCenterX = 0.5;
        this.pointerCenterY = 0.5;
        this.drawWithCurrentState();
        this.callbackOnResetPointerPosition();
    };
    PointerCanvasController.prototype.drawWithCurrentState = function () {
        var ctx = this.canvas.getContext('2d');
        ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        drawCircleInCanvas(this.pointerCenterX, this.pointerCenterY, this.radius, this.canvas);
    };
    return PointerCanvasController;
}());
var ControlPresentationHelper = (function () {
    function ControlPresentationHelper(connectionString, pointerController) {
        this.connectionString = connectionString;
        this.pointerController = pointerController;
        var self = this;
        this.pointerController.callbackOnChangeXY = function (x, y) {
            var obj = {};
            obj[kActionTypeCodeKey] = ActionTypeCode.ChangePointerOriginAction;
            obj[kPointerCenterXKey] = x;
            obj[kPointerCenterYKey] = y;
            self.sendRequestObject(obj);
        };
        this.pointerController.callbackOnDecreasePointerSize = function () {
            self.sendActionCode(ActionTypeCode.DeceasePointerSizeAction);
        };
        this.pointerController.callbackOnIncreasePointerSize = function () {
            self.sendActionCode(ActionTypeCode.IncreasePointerSizeAction);
        };
        this.pointerController.callbackOnResetPointerPosition = function () {
            self.sendActionCode(ActionTypeCode.ResetPointerPositionAction);
        };
        this.pointerController.callbackOnResetPointerSize = function () {
            self.sendActionCode(ActionTypeCode.ResetPointerSizeAction);
        };
        this.pointerController.callbackOnHide = function () {
            self.sendActionCode(ActionTypeCode.HidePointerAction);
        };
        this.pointerController.callbackOnShow = function () {
            self.sendActionCode(ActionTypeCode.ShowPointerAction);
        };
    }
    ControlPresentationHelper.prototype.run = function () {
        this.setupControls();
        this.setupWebSocket();
    };
    ControlPresentationHelper.prototype.setupWebSocket = function () {
        this.ws = new WebSocket("ws://" + location.host);
        var self = this;
        this.ws.onopen = function (ev) {
            self.ws.send(self.connectionString);
        };
        this.ws.onmessage = function (ev) {
            return false;
        };
        this.ws.onerror = function (ev) {
        };
        this.ws.onclose = function (ev) {
        };
    };
    ControlPresentationHelper.prototype.setupControls = function () {
        var previousButton = document.getElementById("previousButton");
        var nextButton = document.getElementById("nextButton");
        var self = this;
        previousButton.onclick = function (ev) {
            self.previousButtonPressed();
        };
        nextButton.onclick = function (ev) {
            self.nextButtonPressed();
        };
    };
    ControlPresentationHelper.prototype.nextButtonPressed = function () {
        this.sendChangePageAction(PageChangeActionType.MoveNext);
    };
    ControlPresentationHelper.prototype.previousButtonPressed = function () {
        this.sendChangePageAction(PageChangeActionType.MovePrevious);
    };
    ControlPresentationHelper.prototype.sendChangePageAction = function (type) {
        var obj = {};
        obj[kActionTypeCodeKey] = ActionTypeCode.PageChangeAction;
        obj[kPageChangeActionTypeKey] = type;
        var request = JSON.stringify(obj);
        this.ws.send(request);
    };
    ControlPresentationHelper.prototype.sendActionCode = function (code) {
        var obj = {};
        obj[kActionTypeCodeKey] = code;
        this.sendRequestObject(obj);
    };
    ControlPresentationHelper.prototype.sendRequestObject = function (obj) {
        var request = JSON.stringify(obj);
        this.ws.send(request);
    };
    return ControlPresentationHelper;
}());
window.addEventListener("load", function () {
    var canvasHelper = new PointerCanvasController();
    window["canvasHelper"] = canvasHelper;
    var helper = new ControlPresentationHelper(window["activationRequestString"], canvasHelper);
    window["helper"] = helper;
    canvasHelper.run();
    helper.run();
});
//# sourceMappingURL=ControlPresentation.js.map