
enum ActionTypeCode {
    PageChangeAction = 1,
    ChangePointerOriginAction = 2,
    IncreasePointerSizeAction = 3,
    DeceasePointerSizeAction = 4,
    ResetPointerSizeAction = 5,
    ShowPointerAction = 6,
    HidePointerAction = 7,
    ResetPointerPositionAction = 8
}

enum PageChangeActionType {
    MoveNext = 1,
    MovePrevious = 2
}

const kActionTypeCodeKey: string = "kActionTypeCodeKey";
const kPageChangeActionTypeKey: string = "kPageChangeActionTypeKey";

const kPointerCenterXKey = "kPointerCenterXKey";
const kPointerCenterYKey = "kPointerCenterYKey";

function absoluteY(element: HTMLElement): number {
    var top = 0, left = 0;
    do {
        top += element.offsetTop || 0;
        left += element.offsetLeft || 0;
        element = element.offsetParent as HTMLElement;
    } while (element);

    return top;
};




function drawCircleInCanvas(pointerCenterX: number, pointerCenterY: number,
    radius: number, canvas: HTMLCanvasElement) {
    var ctx = canvas.getContext('2d');
    ctx.fillStyle = "#FF0000";
    ctx.beginPath();
    var x = pointerCenterX * canvas.width;
    var y = pointerCenterY * canvas.height;

    ctx.arc(x, y, radius, 0, 2 * Math.PI, false);
    ctx.fill();
}

interface JQuery {
    modal(options: any);
}

