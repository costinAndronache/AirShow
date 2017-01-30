///<reference path="Common.ts"/>
var ConnectControlPointerCanvasController = (function () {
    function ConnectControlPointerCanvasController() {
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
    ConnectControlPointerCanvasController.prototype.expand = function () {
        this.isShown = true;
        jQuery("#modalContainer").modal("show");
        this.callbackOnShow();
        document.body.addEventListener("touchmove", this.preventDefaultTouchBehaviour, false);
    };
    ConnectControlPointerCanvasController.prototype.contract = function () {
        this.isShown = false;
        jQuery("#modalContainer").modal("hide");
        document.body.removeEventListener("touchmove", this.preventDefaultTouchBehaviour);
        this.callbackOnHide();
    };
    ConnectControlPointerCanvasController.prototype.resizeCanvasAfterParent = function () {
        this.canvas.width = this.canvasContainer.clientWidth;
        this.canvas.height = this.canvasContainer.clientHeight;
    };
    ConnectControlPointerCanvasController.prototype.toggleDisplayOrHide = function () {
        if (this.isShown) {
            this.contract();
        }
        else {
            this.expand();
        }
    };
    ConnectControlPointerCanvasController.prototype.run = function () {
        this.setupControls();
    };
    ConnectControlPointerCanvasController.prototype.setupControls = function () {
        var self = this;
        var canvasParent = this.canvas.parentElement;
        this.preventDefaultTouchBehaviour = function (ev) {
            if (ev.target == canvasParent) {
                ev.preventDefault();
            }
            return false;
        };
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
            ev.preventDefault();
            var rect = canvasParent.getBoundingClientRect();
            var x = ev.targetTouches[0].pageX - rect.left;
            var y = ev.targetTouches[0].pageY - rect.top;
            redrawWithCoordinates(x, y);
        };
        var pointerHandler = function (ev) {
            ev.preventDefault();
            redrawWithCoordinates(ev.offsetX, ev.offsetY);
        };
        var eventType = "";
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
        console.log(eventType);
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
    ConnectControlPointerCanvasController.prototype.resetPosition = function () {
        this.pointerCenterX = 0.5;
        this.pointerCenterY = 0.5;
        this.drawWithCurrentState();
        this.callbackOnResetPointerPosition();
    };
    ConnectControlPointerCanvasController.prototype.drawWithCurrentState = function () {
        var ctx = this.canvas.getContext('2d');
        ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        drawCircleInCanvas(this.pointerCenterX, this.pointerCenterY, this.radius, this.canvas);
    };
    return ConnectControlPointerCanvasController;
}());
var ConnectControlControlPresentationHelper = (function () {
    function ConnectControlControlPresentationHelper(roomToken, pointerController) {
        this.roomToken = roomToken;
        this.pointerController = pointerController;
        this.defaultSocketErrorHandler = function () {
            jQuery("#modalError").modal("show");
            self.toggleBatteryFriendlyModeTo(true);
        };
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
    ConnectControlControlPresentationHelper.prototype.run = function () {
        this.setupControls();
        this.loadingIndicatorDiv.style.height = "50px";
        var self = this;
        this.makeNewWSConnectionWithCallback(function () {
            self.loadingIndicatorDiv.style.height = "0";
        });
    };
    ConnectControlControlPresentationHelper.prototype.makeNewWSConnectionWithCallback = function (cb) {
        this.ws = new WebSocket("ws://" + location.host);
        var self = this;
        this.ws.onopen = function (ev) {
            var obj = {};
            obj[RoomTokenKey] = self.roomToken;
            obj[SideKey] = ControlSide;
            self.sendRequestObject(obj);
            self.lastActivityTimestamp = Date.now();
            self.isInBatteryFriendlyMode = false;
            self.intervalToken = setInterval(function () {
                var elapsed = Date.now() - self.lastActivityTimestamp;
                if (elapsed >= maxTimeOfInactivity) {
                    alert('Will disconnect due to inactivity');
                    self.ws.send(JSON.stringify({ kActionTypeCodeKey: 10 }));
                    clearInterval(self.intervalToken);
                }
            }, maxTimeOfInactivity);
            if (cb) {
                cb();
            }
        };
        this.ws.onmessage = function (ev) {
            self.ws.onerror = function () { };
            jQuery("#modalSocketDisconnect").modal("show");
            self.ws.close();
        };
        this.ws.onclose = function () {
            self.toggleBatteryFriendlyModeTo(true);
        };
        this.ws.onerror = self.defaultSocketErrorHandler;
    };
    ConnectControlControlPresentationHelper.prototype.setupControls = function () {
        var previousButton = document.getElementById("previousButton");
        var nextButton = document.getElementById("nextButton");
        this.switchConnectionButton = document.getElementById("switchConnectionButton");
        this.loadingIndicatorDiv = document.getElementById("loadingIndicatorDiv");
        var self = this;
        previousButton.onclick = function (ev) {
            self.previousButtonPressed();
        };
        nextButton.onclick = function (ev) {
            self.nextButtonPressed();
        };
        this.switchConnectionButton.onclick = function () {
            var value = !self.isInBatteryFriendlyMode;
            self.toggleBatteryFriendlyModeTo(value);
            if (value) {
                jQuery("#modalAboutBatteryFriendly").modal("show");
            }
        };
    };
    ConnectControlControlPresentationHelper.prototype.nextButtonPressed = function () {
        this.sendChangePageAction(PageChangeActionType.MoveNext);
    };
    ConnectControlControlPresentationHelper.prototype.previousButtonPressed = function () {
        this.sendChangePageAction(PageChangeActionType.MovePrevious);
    };
    ConnectControlControlPresentationHelper.prototype.sendChangePageAction = function (type) {
        var obj = {};
        obj[kActionTypeCodeKey] = ActionTypeCode.PageChangeAction;
        obj[kPageChangeActionTypeKey] = type;
        this.sendRequestObject(obj);
    };
    ConnectControlControlPresentationHelper.prototype.sendActionCode = function (code) {
        var obj = {};
        obj[kActionTypeCodeKey] = code;
        this.sendRequestObject(obj);
    };
    ConnectControlControlPresentationHelper.prototype.sendRequestObject = function (obj) {
        var fps = 1000 / 25;
        var timeElapsed = Date.now() - this.lastActivityTimestamp;
        if (timeElapsed < fps) {
            console.log(timeElapsed);
            return;
        }
        var request = JSON.stringify(obj);
        this.sendControlString(request);
        this.lastActivityTimestamp = Date.now();
    };
    ConnectControlControlPresentationHelper.prototype.sendControlString = function (message) {
        if (!this.isInBatteryFriendlyMode) {
            this.ws.send(message);
        }
        else {
            var xhr = new XMLHttpRequest();
            var uriComponent = encodeURIComponent(message);
            xhr.open("GET", "/Control/SendControlMessage?sessionToken=" + this.roomToken + "&message=" + uriComponent);
            xhr.onreadystatechange = function () {
                if (xhr.readyState === XMLHttpRequest.DONE) {
                    if (xhr.responseText) {
                        var response = JSON.parse(xhr.responseText);
                        if (response["error"]) {
                            alert(xhr.responseText);
                        }
                    }
                }
            };
            xhr.send();
        }
    };
    ConnectControlControlPresentationHelper.prototype.toggleBatteryFriendlyModeTo = function (value) {
        var toggleToolsButton = document.getElementById("toggleToolsButton");
        var self = this;
        this.isInBatteryFriendlyMode = value;
        if (this.isInBatteryFriendlyMode) {
            this.ws.close();
            this.ws.onerror = function () { };
            self.switchConnectionButton.innerHTML = "Reconnect";
            toggleToolsButton.hidden = true;
        }
        else {
            self.switchConnectionButton.hidden = true;
            this.loadingIndicatorDiv.style.height = "50px";
            this.makeNewWSConnectionWithCallback(function () {
                toggleToolsButton.hidden = false;
                self.switchConnectionButton.hidden = false;
                self.switchConnectionButton.innerHTML = "Battery friendly ";
                self.loadingIndicatorDiv.style.height = "0";
            });
        }
    };
    return ConnectControlControlPresentationHelper;
}());
window.addEventListener("load", function () {
    var canvasHelper = new ConnectControlPointerCanvasController();
    window["canvasHelper"] = canvasHelper;
    var helper = new ConnectControlControlPresentationHelper(window["presentationSessionToken"], canvasHelper);
    window["helper"] = helper;
    canvasHelper.run();
    helper.run();
});
//# sourceMappingURL=ConnectControlForPresentation.js.map