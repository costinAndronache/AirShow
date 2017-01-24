var ActionTypeCode;
(function (ActionTypeCode) {
    ActionTypeCode[ActionTypeCode["PageChangeAction"] = 1] = "PageChangeAction";
    ActionTypeCode[ActionTypeCode["ChangePointerOriginAction"] = 2] = "ChangePointerOriginAction";
    ActionTypeCode[ActionTypeCode["IncreasePointerSizeAction"] = 3] = "IncreasePointerSizeAction";
    ActionTypeCode[ActionTypeCode["DeceasePointerSizeAction"] = 4] = "DeceasePointerSizeAction";
    ActionTypeCode[ActionTypeCode["ResetPointerSizeAction"] = 5] = "ResetPointerSizeAction";
    ActionTypeCode[ActionTypeCode["ShowPointerAction"] = 6] = "ShowPointerAction";
    ActionTypeCode[ActionTypeCode["HidePointerAction"] = 7] = "HidePointerAction";
    ActionTypeCode[ActionTypeCode["ResetPointerPositionAction"] = 8] = "ResetPointerPositionAction";
    ActionTypeCode[ActionTypeCode["CloseDueToBeingReplacedAction"] = 9] = "CloseDueToBeingReplacedAction";
    ActionTypeCode[ActionTypeCode["CloseDueToBeingInactive"] = 10] = "CloseDueToBeingInactive";
})(ActionTypeCode || (ActionTypeCode = {}));
var PageChangeActionType;
(function (PageChangeActionType) {
    PageChangeActionType[PageChangeActionType["MoveNext"] = 1] = "MoveNext";
    PageChangeActionType[PageChangeActionType["MovePrevious"] = 2] = "MovePrevious";
})(PageChangeActionType || (PageChangeActionType = {}));
var maxTimeOfInactivity = 1000 * 60 * 15;
var kActionTypeCodeKey = "kActionTypeCodeKey";
var kPageChangeActionTypeKey = "kPageChangeActionTypeKey";
var kPointerCenterXKey = "kPointerCenterXKey";
var kPointerCenterYKey = "kPointerCenterYKey";
var RoomTokenKey = "kRoomTokenKey";
var SideKey = "kSideKey";
var ViewSide = "view";
var ControlSide = "control";
function absoluteY(element) {
    var top = 0, left = 0;
    do {
        top += element.offsetTop || 0;
        left += element.offsetLeft || 0;
        element = element.offsetParent;
    } while (element);
    return top;
}
;
function drawCircleInCanvas(pointerCenterX, pointerCenterY, radius, canvas) {
    var ctx = canvas.getContext('2d');
    ctx.fillStyle = "#FF0000";
    ctx.beginPath();
    var x = pointerCenterX * canvas.width;
    var y = pointerCenterY * canvas.height;
    ctx.arc(x, y, radius, 0, 2 * Math.PI, false);
    ctx.fill();
}
//# sourceMappingURL=Common.js.map