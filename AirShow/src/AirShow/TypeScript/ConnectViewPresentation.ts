
///<reference path="Definitions/PDFJS.d.ts"/>

interface Document {
    //mozFullScreen: boolean
    //msFullscreenElement: boolean
}



class ConnectViewPresentationHelper {

    pointerCenterX: number;
    pointerCenterY: number;

    isShowingPointer: boolean;
    radius: number;

    currentDisplayedDataImage: string;

    pdfURL: string;
    canvasId: string;
    canvas: HTMLCanvasElement;
    pdfFile: PDFDocumentProxy;
    currentDisplayedPage: number;
    fullScreenButton: HTMLButtonElement;

    callbackWhenPdfLoaded: () => void;

    constructor(pdfURL: string, canvasId: string ) {
        this.pdfURL = pdfURL;
        this.canvasId = canvasId;
        this.currentDisplayedPage = -1;
        this.canvas = document.getElementById(this.canvasId) as HTMLCanvasElement;
        this.isShowingPointer = false;
        this.radius = 10;
    }



    run() {
        this.setupControls();

        var loadingIndicatorDiv = document.getElementById("loadingIndicatorDiv") as HTMLDivElement;
        var topCanvasContainer = document.getElementById("topCanvasContainer") as HTMLDivElement;
        topCanvasContainer.hidden = true;
        this.fullScreenButton.hidden = true;

        var self = this;
        PDFJS.workerSrc = "/lib/pdfjs-dist/build/pdf.worker.min.js";
        PDFJS.getDocument(this.pdfURL).then(function (pdfPromise: PDFDocumentProxy) {

            loadingIndicatorDiv.style.height = "0";
            loadingIndicatorDiv.hidden = true;
            self.fullScreenButton.hidden = false;
            topCanvasContainer.hidden = false;

            self.pdfFile = pdfPromise;
            self.callbackWhenPdfLoaded();
            self.nextStepAfterLoadingPDF();
        });
    }


    private nextStepAfterLoadingPDF() {
        this.displayPage(1);
    }

    private drawWithCurrentState() {
        var self = this;
        var ctx = self.canvas.getContext('2d');
        this.drawDataUri(function () {
            if (self.isShowingPointer) {
                drawCircleInCanvas(self.pointerCenterX, self.pointerCenterY, self.radius, self.canvas);
            }

        });

    }

    private drawDataUri(callback: () => void ) {
        var canvas = this.canvas;
        var image = new Image();
        image.src = this.currentDisplayedDataImage;
        image.addEventListener("load", function () {
            canvas.getContext('2d').drawImage(image, 0, 0);
            callback();
        });
    }

    private displayPage(index: number) {
        var self = this;
        this.pdfFile.getPage(index).then(function (page: PDFPageProxy) {
   
            var context = self.canvas.getContext('2d');
            var viewport = page.getViewport(1);

            viewport = page.getViewport(1.0);

            var parentDiv = self.canvas.parentElement as HTMLDivElement;
            self.canvas.height = viewport.height ;
            self.canvas.width = viewport.width;

            var renderContext = {
                canvasContext: context,
                viewport: viewport
            };
            page.render(renderContext).then(function (page: PDFPageProxy) {
                self.currentDisplayedDataImage = self.canvas.toDataURL();
                self.pointerCenterX = 0.5;
                self.pointerCenterY = 0.5;

                self.drawWithCurrentState();
            });
            self.currentDisplayedPage = index;
        });
    }

    displayNextPage() {
        if (this.currentDisplayedPage < this.pdfFile.numPages) {
            this.displayPage(this.currentDisplayedPage + 1);
        }
    }

    displayPreviousPage() {
        if (this.currentDisplayedPage > 1) {
            this.displayPage(this.currentDisplayedPage - 1);
        }
    }

    private setupControls() {
        this.fullScreenButton = document.getElementById("fullScreenButton") as HTMLButtonElement;

        var self = this;

        this.fullScreenButton.onclick = function (ev: Event) {
            if (document.fullscreenElement) {
                if (document.exitFullscreen) {
                    document.exitFullscreen();
                }
            } else {
                if (!self.makeCanvasFullScreen())
                    alert('This browser does not have full-screen capabilities');
            }
        }

        window.addEventListener('keydown', function (ev: KeyboardEvent) {
            if (ev.keyCode == 37) { // left arrow 
                self.displayPreviousPage();
                return false;
            }

            if (ev.keyCode == 39) { // right arrow
                self.displayNextPage();
                return false;
            }

        });
    }



    private makeCanvasFullScreen(): boolean {
        var cv = this.canvas;

        var adjustWidthHeight = function () {
            cv.style.height = "100%";
            cv.style.width = "auto";
        }

        var oldHeight = cv.style.height;
        var oldWidth = cv.style.width;

        var whenExitingFullScreen = function () {
            cv.style.width = oldWidth;
            cv.style.height = oldHeight;
        }

        if (cv.requestFullscreen) {
            cv.requestFullscreen();
            adjustWidthHeight();
            return true
        }

        var self = this;
        var exitHandler = function () {
            if (document.webkitIsFullScreen || document.fullscreenElement || document.mozFullScreen || document.msFullscreenElement) {
            } else {
                whenExitingFullScreen();
                document.removeEventListener('webkitfullscreenchange', this, false);
                document.removeEventListener('mozfullscreenchange', this, false);
                document.removeEventListener('fullscreenchange', this, false);
                document.removeEventListener('MSFullscreenChange', this, false);
            }
        }

        if (document.addEventListener) {
            document.addEventListener('webkitfullscreenchange', exitHandler, false);
            document.addEventListener('mozfullscreenchange', exitHandler, false);
            document.addEventListener('fullscreenchange', exitHandler, false);
            document.addEventListener('MSFullscreenChange', exitHandler, false);
        }

        if (cv.webkitRequestFullScreen) {
            cv.webkitRequestFullScreen();
            adjustWidthHeight();

            return true
        }


        return false;

    }

    showPointer() {
        this.isShowingPointer = true;
        this.drawWithCurrentState();
    }

    hidePointer() {
        this.isShowingPointer = false;
        this.drawWithCurrentState();
    }

    changePointerXY(x: number, y: number) {
        this.pointerCenterX = x;
        this.pointerCenterY = y;
        this.drawWithCurrentState();
    }

    increasePointerSize() {
        this.radius += 5;
        this.drawWithCurrentState();
    }

    decreasePointerSize() {
        this.radius -= 5;
        if (this.radius <= 5) {
            this.radius = 5;
        }
        this.drawWithCurrentState();
    }

    resetPointerSize() {
        this.radius = 10;
        this.drawWithCurrentState();
    }

    resetPointerPosition() {
        this.pointerCenterX = 0.5;
        this.pointerCenterY = 0.5;
        this.drawWithCurrentState();
    }

}

class ConnectPresentationControllerHelper {

    private presentationHelper: ConnectViewPresentationHelper;
    private presentationId: string;
    private ws: WebSocket;

    constructor(presentationId: string, presentationHelper: ConnectViewPresentationHelper) {
        this.presentationId = presentationId;
        this.presentationHelper = presentationHelper;
    }


    run() {
        var xhr = new XMLHttpRequest();
        xhr.open("POST", window.location.origin + "/Control/ConnectViewForPresentation?presentationId=" + this.presentationId);
        var self = this;

        xhr.onreadystatechange = function (ev: ProgressEvent) {
            if (xhr.readyState === XMLHttpRequest.DONE) {
                var response = JSON.parse(xhr.responseText);
                if (response["error"]) {
                    alert(xhr.responseText);
                } 

                if (response["roomToken"]) {
                    alert(response["roomToken"]);
                    self.connectWSWithRoomToken(response["roomToken"]);
                }
            }
        }
        xhr.send();
    }

    private connectWSWithRoomToken(token: string) {
        var self = this;
        this.ws = new WebSocket("ws://" + location.host);
        this.ws.onopen = function (ev: Event) {
            var obj: any = {};
            obj[RoomTokenKey] = token;
            obj[SideKey] = ViewSide;
            var message = JSON.stringify(obj);
            self.ws.send(message);
            
            jQuery("#modalMessageView").modal("show");

        };

        this.ws.onerror = function (ev: Event) {
        };

        this.ws.onclose = function (ev: CloseEvent) {
        }

        this.ws.onmessage = function (ev: MessageEvent) {
            var message = JSON.parse(ev.data);
            self.handleMessage(message);
        };
    }

     handleMessage(message: any) {
        var messageCode = message[kActionTypeCodeKey] as number;
        if (messageCode == ActionTypeCode.PageChangeAction) {
            this.handleChangePageMessage(message);
        }
        console.log(message);
        switch (messageCode) {
            case ActionTypeCode.IncreasePointerSizeAction:
                this.presentationHelper.increasePointerSize();
                break;
            case ActionTypeCode.DeceasePointerSizeAction:
                this.presentationHelper.decreasePointerSize();
                break;
            case ActionTypeCode.ResetPointerPositionAction:
                this.presentationHelper.resetPointerPosition();
                break;
            case ActionTypeCode.ChangePointerOriginAction:
                this.handleChangeXYMessage(message);
                break;
            case ActionTypeCode.ShowPointerAction:
                this.presentationHelper.showPointer();
                break;
            case ActionTypeCode.HidePointerAction:
                this.presentationHelper.hidePointer();
                break;
            case ActionTypeCode.ResetPointerSizeAction:
                this.presentationHelper.resetPointerSize();
                break;
            case ActionTypeCode.CloseDueToBeingReplacedAction:
                this.handleDismiss();
                break;
            default: break;
        }
    }

     private handleDismiss() {
         this.ws.close();
         alert('You have been dismissed by another party');
     }

    private handleChangePageMessage(message: any) {
        var changePageTypeCode = message[kPageChangeActionTypeKey] as number;
        if (changePageTypeCode == PageChangeActionType.MoveNext) {
            this.presentationHelper.displayNextPage();
        } else {
            this.presentationHelper.displayPreviousPage();
            
        }
    }

    private handleShowPointerMessage(message: any) {
        this.presentationHelper.showPointer();
    }

    private handleHidePointerMessage(message: any) {
        this.presentationHelper.hidePointer();
    }

    private handleChangeXYMessage(message: any) {
        var x = message[kPointerCenterXKey] as number;
        var y = message[kPointerCenterYKey] as number;
        this.presentationHelper.changePointerXY(x, y);
    }

}


class ConnectActivationHelper {

    private presentationHelper: ConnectViewPresentationHelper
    private controllerHelper: ConnectPresentationControllerHelper
    constructor(presentationHelper: ConnectViewPresentationHelper) {
        this.presentationHelper = presentationHelper;
    }

    run() {

        var self = this;
        var activateButton = document.getElementById("activateButton") as HTMLButtonElement;
        activateButton.onclick = function (ev: Event) {
            self.controllerHelper = new ConnectPresentationControllerHelper(window["activationRequestString"], self.presentationHelper);
            activateButton.hidden = true;
            self.controllerHelper.run();
        }

        activateButton.hidden = true;
        this.presentationHelper.callbackWhenPdfLoaded = function () {
            activateButton.hidden = false;
        }

    }
}


window.addEventListener("load", function () {
    let greeter = new ConnectViewPresentationHelper(window["presentationURL"], "pdfHost");
    let activationHelper = new ConnectActivationHelper(greeter);
    greeter.run();
    activationHelper.run();
    window["activationHelper"] = activationHelper;
});

