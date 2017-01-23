///<reference path="Common.ts"/>


class PointerCanvasController {

    pointerCenterX: number;
    pointerCenterY: number;
    radius: number;

    callbackOnResetPointerPosition: () => void;
    callbackOnResetPointerSize: () => void;
    callbackOnChangeXY: (x: number, y: number) => void;
    callbackOnIncreasePointerSize: () => void;
    callbackOnDecreasePointerSize: () => void;
    

    canvas: HTMLCanvasElement;
    toolsDiv: HTMLDivElement;
    canvasContainer: HTMLDivElement;


    constructor() {
        this.canvas = document.getElementById("pointerCanvas") as HTMLCanvasElement;
        this.pointerCenterX = 0.5;
        this.pointerCenterY = 0.5;
        this.canvasContainer = document.getElementById("pointerCanvasContainer") as HTMLDivElement;
        this.toolsDiv = document.getElementById("pointerControlsContainer") as HTMLDivElement;

        this.resizeCanvasAfterParent();

        this.radius = 10;
    }

    expand() {
        this.canvasContainer.style.height = window.innerHeight + 'px';
        this.toolsDiv.style.height = "auto";
        this.resizeCanvasAfterParent();
        this.drawWithCurrentState();
    }

    contract() {
        this.canvasContainer.style.height = "0";
        this.toolsDiv.style.height = "0";
        this.resizeCanvasAfterParent();
    }

    private resizeCanvasAfterParent() {
        this.canvas.width = this.canvasContainer.clientWidth;
        this.canvas.height = this.canvasContainer.clientHeight;
    }

    run() {
        this.setupControls();
    }

    private setupControls() {
        var self = this;
        var canvasParent = this.canvas.parentElement as HTMLDivElement;

        var redrawWithCoordinates = function (coordX: number, coordY: number) {
            self.pointerCenterX = coordX / self.canvas.width;
            self.pointerCenterY = coordY / self.canvas.height;

            self.callbackOnChangeXY(self.pointerCenterX, self.pointerCenterY);
            self.drawWithCurrentState();
        }

        canvasParent.addEventListener("touchmove", function (ev: TouchEvent) {
            var touch = ev.targetTouches[0];
            var x = touch.clientX - self.canvas.clientLeft;
            var y = touch.clientY - self.canvas.clientTop;
            redrawWithCoordinates(x, y);
        }); 

        canvasParent.addEventListener("mousemove", function (ev: MouseEvent) {
            if (ev.buttons == 1) {
            redrawWithCoordinates(ev.offsetX, ev.offsetY);
            }
        });


        var resetSizeButton = document.getElementById("resetSizeButton") as HTMLButtonElement;
        var resetPositionButton = document.getElementById("resetPositionButton") as HTMLButtonElement;
        var increaseSizeButton = document.getElementById("increaseSizeButton") as HTMLButtonElement;
        var decreaseSizeButton = document.getElementById("decreaseSizeButton") as HTMLButtonElement;

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

class ControlPresentationHelper {

    private ws: WebSocket;
    private connectionString: string;
    private areToolsExpanded: boolean;

    private pointerController: PointerCanvasController;

    constructor(connectionString: string, pointerController: PointerCanvasController) {
        this.connectionString = connectionString;
        this.areToolsExpanded = false;
        this.pointerController = pointerController;
        this.pointerController.contract();

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

    }

    run() {
        this.setupControls();
        this.setupWebSocket();
    }


    private setupWebSocket() {
        this.ws = new WebSocket("ws://" + location.host);
        var self = this;

        this.ws.onopen = function (ev: Event) {
            self.ws.send(self.connectionString);
        }

        this.ws.onmessage = function (ev: MessageEvent) {
            return false;
        }

        this.ws.onerror = function (ev: Event) {
        }

        this.ws.onclose = function (ev: CloseEvent) {
        }
    }


    private setupControls() {
        var previousButton = document.getElementById("previousButton") as HTMLButtonElement;
        var nextButton = document.getElementById("nextButton") as HTMLButtonElement;
        var toggleButton = document.getElementById("toggleToolsButton") as HTMLButtonElement;
        var self = this;

        previousButton.onclick = function (ev: Event) {
            self.previousButtonPressed();
        };

        nextButton.onclick = function (ev: Event) {
            self.nextButtonPressed();
        }

        toggleButton.onclick = function () {
            self.toggleTools();
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
        var request = JSON.stringify(obj);
        this.ws.send(request);
    }

    private sendActionCode(code: number) {
        var obj: any = {};
        obj[kActionTypeCodeKey] = code;
        this.sendRequestObject(obj);
    }

    private sendRequestObject(obj: any) {
        var request = JSON.stringify(obj);
        this.ws.send(request);
    }

    private toggleTools() {

        var obj: any = {}
        if (this.areToolsExpanded) {
            this.pointerController.contract();
            obj[kActionTypeCodeKey] = ActionTypeCode.HidePointerAction;;
        } else {
            this.pointerController.expand();
            obj[kActionTypeCodeKey] = ActionTypeCode.ShowPointerAction;
        }

        this.areToolsExpanded = !this.areToolsExpanded;
        this.sendRequestObject(obj);
    }

}

window.addEventListener("load", function () {

    var canvasHelper = new PointerCanvasController();
    window["canvasHelper"] = canvasHelper;

    var helper = new ControlPresentationHelper(window["activationRequestString"], canvasHelper);
    window["helper"] = helper;

    canvasHelper.run();
    helper.run();
});