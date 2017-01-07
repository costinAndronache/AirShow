///<reference path="Common.ts"/>

class ControlPresentationHelper {

    private ws: WebSocket;
    private connectionString: string;

    constructor(connectionString: string) {
        this.connectionString = connectionString;
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
            alert("An error ocurred");
        }

        this.ws.onclose = function (ev: CloseEvent) {
            alert("Websocket closed? " + ev.code);
        }
    }


    private setupControls() {
        var previousButton = document.getElementById("previousButton") as HTMLButtonElement;
        var nextButton = document.getElementById("nextButton") as HTMLButtonElement;
        var self = this;
        previousButton.onclick = function (ev: Event) {
            self.previousButtonPressed();
        };

        nextButton.onclick = function (ev: Event) {
            self.nextButtonPressed();
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

}

window.addEventListener("load", function () {
    var helper = new ControlPresentationHelper(window["activationRequestString"]);
    window["helper"] = helper;
    helper.run();
});