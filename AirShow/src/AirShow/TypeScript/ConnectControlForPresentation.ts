///<reference path="Common.ts"/>


class ConnectControlPointerCanvasController {

    pointerCenterX: number;
    pointerCenterY: number;
    radius: number;

    private isShown: boolean;

    callbackOnResetPointerPosition: () => void;
    callbackOnResetPointerSize: () => void;
    callbackOnChangeXY: (x: number, y: number) => void;
    callbackOnIncreasePointerSize: () => void;
    callbackOnDecreasePointerSize: () => void;
    callbackOnHide: () => void;
    callbackOnShow: () => void;

    private preventDefaultTouchBehaviour: (ev: TouchEvent) => void;

    canvas: HTMLCanvasElement;
    toolsDiv: HTMLDivElement;
    canvasContainer: HTMLDivElement;

    modalDivContainer: HTMLDivElement;

    constructor() {
        this.canvas = document.getElementById("pointerCanvas") as HTMLCanvasElement;
        this.pointerCenterX = 0.5;
        this.pointerCenterY = 0.5;
        this.canvasContainer = document.getElementById("pointerCanvasContainer") as HTMLDivElement;
        this.toolsDiv = document.getElementById("pointerControlsContainer") as HTMLDivElement;
        this.modalDivContainer = document.getElementById("modalContainer") as HTMLDivElement;
        this.resizeCanvasAfterParent();

        this.isShown = false;

        this.radius = 10;


    }

    expand() {

        this.isShown = true;
        jQuery("#modalContainer").modal("show");
        this.callbackOnShow();
        document.body.addEventListener("touchmove", this.preventDefaultTouchBehaviour, false);
    }

    contract() {
        this.isShown = false;
        jQuery("#modalContainer").modal("hide");
        document.body.removeEventListener("touchmove", this.preventDefaultTouchBehaviour);
        this.callbackOnHide();
    }

    private resizeCanvasAfterParent() {
        this.canvas.width = this.canvasContainer.clientWidth;
        this.canvas.height = this.canvasContainer.clientHeight;
    }

    toggleDisplayOrHide() {
        if (this.isShown) {
            this.contract();
        } else {
            this.expand();
        }
    } 

    run() {
        this.setupControls();
    }

    private setupControls() {
        var self = this;
        var canvasParent = this.canvas.parentElement as HTMLDivElement;

        this.preventDefaultTouchBehaviour = function (ev: TouchEvent) {
            if (ev.target == canvasParent) {
                ev.preventDefault();
            }
            return false;
        }

        window.addEventListener("resize", function (ev: UIEvent) {
            self.resizeCanvasAfterParent();
            self.drawWithCurrentState();
        });

        var redrawWithCoordinates = function (coordX: number, coordY: number) {
            self.pointerCenterX = coordX / self.canvas.width;
            self.pointerCenterY = coordY / self.canvas.height;

            self.callbackOnChangeXY(self.pointerCenterX, self.pointerCenterY);
            self.drawWithCurrentState();
        }

        var touchMoveHandler = function (ev: TouchEvent) {
            ev.preventDefault();

            var rect = canvasParent.getBoundingClientRect();
            var x = ev.targetTouches[0].pageX - rect.left;
            var y = ev.targetTouches[0].pageY - rect.top;

            redrawWithCoordinates(x, y);
        }

        var pointerHandler = function (ev: PointerEvent) {
            ev.preventDefault();

            redrawWithCoordinates(ev.offsetX, ev.offsetY);
        }

        var eventType: string = "";
        if (window.navigator.pointerEnabled) {
            eventType = "pointermove";
            canvasParent.addEventListener(eventType, pointerHandler, false);
        } else if (window.navigator.msPointerEnabled) {
            eventType = "MSPointerMove";
            canvasParent.addEventListener(eventType, pointerHandler, false);
        } else {
            eventType = "touchmove";
            canvasParent.addEventListener(eventType, touchMoveHandler, false); 
        }

        console.log(eventType);
        //if (eventType.length == 0) {
            canvasParent.addEventListener("mousemove", function (ev: MouseEvent) {
                if (ev.buttons == 1) {
                    redrawWithCoordinates(ev.offsetX, ev.offsetY);
                }
            });
        //}
        

        jQuery("#modalContainer").on("hidden.bs.modal", function () {
            self.isShown = false;
            jQuery("#modalContainer").modal("hide");
            self.callbackOnHide();
        });

        jQuery("#modalContainer").on("shown.bs.modal", function () {
            self.resizeCanvasAfterParent();
            self.drawWithCurrentState();
        });

        var resetSizeButton = document.getElementById("resetSizeButton") as HTMLButtonElement;
        var resetPositionButton = document.getElementById("resetPositionButton") as HTMLButtonElement;
        var increaseSizeButton = document.getElementById("increaseSizeButton") as HTMLButtonElement;
        var decreaseSizeButton = document.getElementById("decreaseSizeButton") as HTMLButtonElement;
        var toggleButton = document.getElementById("toggleToolsButton") as HTMLButtonElement;


        toggleButton.onclick = function () {
            self.toggleDisplayOrHide();
        }
        resetPositionButton.onclick = function () {
            self.resetPosition();
        }

        resetSizeButton.onclick = function () {
            self.radius = 10;
            self.callbackOnResetPointerSize();
            self.drawWithCurrentState();
        }

        increaseSizeButton.onclick = function() {
            self.radius += 5;
            self.callbackOnIncreasePointerSize();
            self.drawWithCurrentState();
        }

        decreaseSizeButton.onclick = function () {
            if (self.radius <= 5) {
                return
            }
            self.radius -= 5;
            self.callbackOnDecreasePointerSize();
            self.drawWithCurrentState();
        }
    }


    private resetPosition() {
        this.pointerCenterX = 0.5;
        this.pointerCenterY = 0.5;
        this.drawWithCurrentState();
        this.callbackOnResetPointerPosition();
    }

    private drawWithCurrentState() {
        var ctx = this.canvas.getContext('2d');
        ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        drawCircleInCanvas(this.pointerCenterX, this.pointerCenterY, this.radius, this.canvas);
    }
}

class ConnectControlControlPresentationHelper {

    private ws: WebSocket;
    private roomToken: string;
    private pointerController: ConnectControlPointerCanvasController;
    private intervalToken: number;

    private lastActivityTimestamp: number;
    private isInBatteryFriendlyMode: boolean;
    private loadingIndicatorDiv: HTMLDivElement;
    private switchConnectionButton: HTMLButtonElement;

    private defaultSocketErrorHandler: () => void;

    constructor(roomToken: string, pointerController: ConnectControlPointerCanvasController) {
        this.roomToken = roomToken;
        this.pointerController = pointerController;

        this.defaultSocketErrorHandler = function () {
            jQuery("#modalError").modal("show");
            self.toggleBatteryFriendlyModeTo(true);
        }

        var self = this;


        this.pointerController.callbackOnChangeXY = function (x: number, y: number) {
            var obj: any = {};
            obj[kActionTypeCodeKey] = ActionTypeCode.ChangePointerOriginAction;
            obj[kPointerCenterXKey] = x;
            obj[kPointerCenterYKey] = y;
            self.sendRequestObject(obj);
        }

        this.pointerController.callbackOnDecreasePointerSize = function () {
            self.sendActionCode(ActionTypeCode.DeceasePointerSizeAction);  
        }

        this.pointerController.callbackOnIncreasePointerSize = function () {
            self.sendActionCode(ActionTypeCode.IncreasePointerSizeAction);
        }

        this.pointerController.callbackOnResetPointerPosition = function () {
            self.sendActionCode(ActionTypeCode.ResetPointerPositionAction);
        }

        this.pointerController.callbackOnResetPointerSize = function () {
            self.sendActionCode(ActionTypeCode.ResetPointerSizeAction);
        }

        this.pointerController.callbackOnHide = function () {
            self.sendActionCode(ActionTypeCode.HidePointerAction);
        }

        this.pointerController.callbackOnShow = function () {
            self.sendActionCode(ActionTypeCode.ShowPointerAction);
        }
    }

    run() {
        this.setupControls();
        this.loadingIndicatorDiv.style.height = "50px";
        var self = this;

        this.makeNewWSConnectionWithCallback(function () {
            self.loadingIndicatorDiv.style.height = "0";
        });
    }


    makeNewWSConnectionWithCallback(cb: () => void) {
        this.ws = new WebSocket("ws://" + location.host);
        var self = this;

        this.ws.onopen = function (ev: Event) {
            var obj: any = {}
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
                    //will get disconnected
                }

            }, maxTimeOfInactivity);

            if (cb) {
                cb();
            }
        }

        this.ws.onmessage = function (ev: MessageEvent) {
            self.ws.onerror = function () { }
            jQuery("#modalSocketDisconnect").modal("show");
            self.ws.close();
        }


        this.ws.onerror = self.defaultSocketErrorHandler;
    }


    private setupControls() {
        var previousButton = document.getElementById("previousButton") as HTMLButtonElement;
        var nextButton = document.getElementById("nextButton") as HTMLButtonElement;
        this.switchConnectionButton = document.getElementById("switchConnectionButton") as HTMLButtonElement;
        this.loadingIndicatorDiv = document.getElementById("loadingIndicatorDiv") as HTMLDivElement;

        var self = this;

        previousButton.onclick = function (ev: Event) {
            self.previousButtonPressed();
        };

        nextButton.onclick = function (ev: Event) {
            self.nextButtonPressed();
        }

        this.switchConnectionButton.onclick = function () {

            var value = !self.isInBatteryFriendlyMode;
            self.toggleBatteryFriendlyModeTo(value);
            if (value) {
                jQuery("#modalAboutBatteryFriendly").modal("show");
            }
        }

    }

    private nextButtonPressed() {
        this.sendChangePageAction(PageChangeActionType.MoveNext);
    }

    private previousButtonPressed() {
        this.sendChangePageAction(PageChangeActionType.MovePrevious);
    }

    private sendChangePageAction(type: PageChangeActionType) {

        var obj: any = {};
        obj[kActionTypeCodeKey] = ActionTypeCode.PageChangeAction;
        obj[kPageChangeActionTypeKey] = type;
        this.sendRequestObject(obj);

    }

    private sendActionCode(code: number) {
        var obj: any = {};
        obj[kActionTypeCodeKey] = code;
        this.sendRequestObject(obj);
    }

    private sendRequestObject(obj: any) {

        var fps = 1000 / 25;
        var timeElapsed = Date.now() - this.lastActivityTimestamp;
        if (timeElapsed < fps) {
            console.log(timeElapsed);
            return
        }

        var request = JSON.stringify(obj);
        this.sendControlString(request);

        this.lastActivityTimestamp = Date.now();
    }

    private sendControlString(message: string) {
        if (!this.isInBatteryFriendlyMode) {
            this.ws.send(message);
        } else {
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
            }

            xhr.send();
        }
    }

    private toggleBatteryFriendlyModeTo(value: boolean) {
        var toggleToolsButton = document.getElementById("toggleToolsButton") as HTMLButtonElement;
        var self = this;
        this.isInBatteryFriendlyMode = value;

        if (this.isInBatteryFriendlyMode) {
            this.ws.close();
            this.ws.onerror = function () { }

            self.switchConnectionButton.innerHTML = "Reconnect";
            toggleToolsButton.hidden = true;

        } else {
            self.switchConnectionButton.hidden = true;
            this.loadingIndicatorDiv.style.height = "50px";

            this.makeNewWSConnectionWithCallback(function () {
                toggleToolsButton.hidden = false;
                self.switchConnectionButton.hidden = false;
                self.switchConnectionButton.innerHTML = "Battery friendly ";
                self.loadingIndicatorDiv.style.height = "0";
            });
        }
    }

}

window.addEventListener("load", function () {

    var canvasHelper = new ConnectControlPointerCanvasController();
    window["canvasHelper"] = canvasHelper;

    var helper = new ConnectControlControlPresentationHelper(window["presentationSessionToken"], canvasHelper);
    window["helper"] = helper;



    canvasHelper.run();
    helper.run();
});