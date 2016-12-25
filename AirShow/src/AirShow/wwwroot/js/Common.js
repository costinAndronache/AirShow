var ActionTypeCode;
(function (ActionTypeCode) {
    ActionTypeCode[ActionTypeCode["PageChangeAction"] = 1] = "PageChangeAction";
})(ActionTypeCode || (ActionTypeCode = {}));
var PageChangeActionType;
(function (PageChangeActionType) {
    PageChangeActionType[PageChangeActionType["MoveNext"] = 1] = "MoveNext";
    PageChangeActionType[PageChangeActionType["MovePrevious"] = 2] = "MovePrevious";
})(PageChangeActionType || (PageChangeActionType = {}));
var kActionTypeCodeKey = "kActionTypeCodeKey";
var kPageChangeActionTypeKey = "kPageChangeActionTypeKey";
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
//# sourceMappingURL=Common.js.map