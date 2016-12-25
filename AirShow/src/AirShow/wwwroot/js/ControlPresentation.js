///<reference path="Common.ts"/>
var ControlPresentationHelper = (function () {
    function ControlPresentationHelper(connectionString) {
        this.connectionString = connectionString;
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
            alert("An error ocurred");
        };
        this.ws.onclose = function (ev) {
            alert("Websocket closed? " + ev.code);
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
    return ControlPresentationHelper;
}());
window.onload = function (ev) {
    var helper = new ControlPresentationHelper(window["activationRequestString"]);
    window["helper"] = helper;
    helper.run();
};
//# sourceMappingURL=ControlPresentation.js.map