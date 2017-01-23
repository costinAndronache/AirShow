///<reference path="Common.ts"/>
var PointerCanvasController = (function () {
    function PointerCanvasController() {
        this.canvas = document.getElementById("pointerCanvas");
        this.pointerCenterX = 0.5;
        this.pointerCenterY = 0.5;
        this.canvasContainer = document.getElementById("pointerCanvasContainer");
        this.toolsDiv = document.getElementById("pointerControlsContainer");
        this.resizeCanvasAfterParent();
        this.radius = 10;
    }
    PointerCanvasController.prototype.expand = function () {
        this.canvasContainer.style.height = window.innerHeight + 'px';
        this.toolsDiv.style.height = "auto";
        this.resizeCanvasAfterParent();
        this.drawWithCurrentState();
    };
    PointerCanvasController.prototype.contract = function () {
        this.canvasContainer.style.height = "0";
        this.toolsDiv.style.height = "0";
        this.resizeCanvasAfterParent();
    };
    PointerCanvasController.prototype.resizeCanvasAfterParent = function () {
        this.canvas.width = this.canvasContainer.clientWidth;
        this.canvas.height = this.canvasContainer.clientHeight;
    };
    PointerCanvasController.prototype.run = function () {
        this.setupControls();
    };
    PointerCanvasController.prototype.setupControls = function () {
        var self = this;
        var canvasParent = this.canvas.parentElement;
        document.body.addEventListener("touchstart", function (ev) {
            if (ev.target == canvasParent) {
                ev.preventDefault();
            }
        }, false);
        document.body.addEventListener("touchend", function (ev) {
            if (ev.target == canvasParent) {
                ev.preventDefault();
            }
        }, false);
        document.body.addEventListener("touchmove", function (ev) {
            if (ev.target == canvasParent) {
                ev.preventDefault();
            }
        }, false);
        var redrawWithCoordinates = function (coordX, coordY) {
            self.pointerCenterX = coordX / self.canvas.width;
            self.pointerCenterY = coordY / self.canvas.height;
            self.callbackOnChangeXY(self.pointerCenterX, self.pointerCenterY);
            self.drawWithCurrentState();
        };
        canvasParent.addEventListener("touchmove", function (ev) {
            var touch = ev.targetTouches[0];
            var x = touch.clientX - self.canvas.clientLeft;
            var y = touch.clientY - self.canvas.clientTop;
            redrawWithCoordinates(x, y);
        });
        canvasParent.addEventListener("mousemove", function (ev) {
            if (ev.buttons == 1) {
                redrawWithCoordinates(ev.offsetX, ev.offsetY);
            }
        });
        var resetSizeButton = document.getElementById("resetSizeButton");
        var resetPositionButton = document.getElementById("resetPositionButton");
        var increaseSizeButton = document.getElementById("increaseSizeButton");
        var decreaseSizeButton = document.getElementById("decreaseSizeButton");
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
        this.areToolsExpanded = false;
        this.pointerController = pointerController;
        this.pointerController.contract();
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
        var toggleButton = document.getElementById("toggleToolsButton");
        var self = this;
        previousButton.onclick = function (ev) {
            self.previousButtonPressed();
        };
        nextButton.onclick = function (ev) {
            self.nextButtonPressed();
        };
        toggleButton.onclick = function () {
            self.toggleTools();
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
    ControlPresentationHelper.prototype.toggleTools = function () {
        var obj = {};
        if (this.areToolsExpanded) {
            this.pointerController.contract();
            obj[kActionTypeCodeKey] = ActionTypeCode.HidePointerAction;
            ;
        }
        else {
            this.pointerController.expand();
            obj[kActionTypeCodeKey] = ActionTypeCode.ShowPointerAction;
        }
        this.areToolsExpanded = !this.areToolsExpanded;
        this.sendRequestObject(obj);
    };
    return ControlPresentationHelper;
}());
window.addEventListener("load", function () {
    document.addEventListener("touchmove", function (ev) {
        ev.preventDefault();
    });
    var canvasHelper = new PointerCanvasController();
    window["canvasHelper"] = canvasHelper;
    var helper = new ControlPresentationHelper(window["activationRequestString"], canvasHelper);
    window["helper"] = helper;
    canvasHelper.run();
    helper.run();
});
//# sourceMappingURL=ControlPresentation.js.map