
enum ActionTypeCode {
    PageChangeAction = 1
}

enum PageChangeActionType {
    MoveNext = 1,
    MovePrevious = 2
}

const kActionTypeCodeKey: string = "kActionTypeCodeKey";
const kPageChangeActionTypeKey: string = "kPageChangeActionTypeKey";

function absoluteY(element: HTMLElement): number {
    var top = 0, left = 0;
    do {
        top += element.offsetTop || 0;
        left += element.offsetLeft || 0;
        element = element.offsetParent as HTMLElement;
    } while (element);

    return top;
};