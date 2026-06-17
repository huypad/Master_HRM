import { Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActionNotificationComponent } from '../../_shared/action-natification/action-notification.component';

import { DeleteEntityDialogComponent } from '../../_shared/delete-entity-dialog/delete-entity-dialog.component';
import { FetchEntityDialogComponent } from '../../_shared/fetch-entity-dialog/fetch-entity-dialog.component';
import { UpdateStatusDialogComponent } from '../../_shared/update-status-dialog/update-status-dialog.component';

export enum MessageType {
	Create,
	Read,
	Update,
	Delete
}

@Injectable({
	providedIn: 'root',
})
export class LayoutUtilsService {
	constructor(
		private snackBar: MatSnackBar,
		private dialog: MatDialog,
	) { }

	// SnackBar for notifications
	showActionNotification(
		message: string,
		type: MessageType = MessageType.Create,
		duration: number = 100000,
		showCloseButton: boolean = true,
		showUndoButton: boolean = false,
		undoButtonDuration: number = 3000,
		verticalPosition: 'top' | 'bottom' = 'top',
		mean: 0 | 1 = 1
	) {
		return this.snackBar.openFromComponent(ActionNotificationComponent, {
			duration: duration,
			data: {
				message,
				snackBar: this.snackBar,
				showCloseButton: showCloseButton,
				showUndoButton: showUndoButton,
				undoButtonDuration,
				verticalPosition,
				type,
				action: 'Undo',
				meanMes: mean
			},
			verticalPosition: verticalPosition,

		});
	}
	showInfo(message: string) {
		let type: MessageType = MessageType.Create,
			duration: number = 100000,
			showCloseButton: boolean = true,
			showUndoButton: boolean = false,
			undoButtonDuration: number = 3000,
			verticalPosition: 'top' | 'bottom' = 'top',
			mean: 0 | 1 = 1;
		return this.snackBar.openFromComponent(ActionNotificationComponent, {
			duration: duration,
			data: {
				message,
				snackBar: this.snackBar,
				showCloseButton: showCloseButton,
				showUndoButton: showUndoButton,
				undoButtonDuration,
				verticalPosition,
				type,
				action: 'Undo',
				meanMes: mean
			},
			verticalPosition: verticalPosition
		});
	}

	showError(message: string) {
		let type: MessageType = MessageType.Read,
			duration: number = 100000,
			showCloseButton: boolean = true,
			showUndoButton: boolean = false,
			undoButtonDuration: number = 3000,
			verticalPosition: 'top' | 'bottom' = 'top';
		return this.snackBar.openFromComponent(ActionNotificationComponent, {
			duration: duration,
			data: {
				message,
				snackBar: this.snackBar,
				showCloseButton: showCloseButton,
				showUndoButton: showUndoButton,
				undoButtonDuration,
				verticalPosition,
				type,
				action: 'Undo',
				meanMes: 0
			},
			verticalPosition: verticalPosition
		});
	}

	// Method returns instance of MatDialog
	deleteElement(title: string = '', description: string = '', waitDesciption: string = '', doPositiveBtn: string = 'Delete') {
		return this.dialog.open(DeleteEntityDialogComponent, {
			data: { title, description, waitDesciption, doPositiveBtn },
			width: '440px',
			panelClass: 'no-padding'
		});
	}

	// Method returns instance of MatDialog
	fetchElements(_data: any) {
		return this.dialog.open(FetchEntityDialogComponent, {
			data: _data,
			width: '400px'
		});
	}

	// Method returns instance of MatDialog
	updateStatusForCustomers(title: string, statuses: any[], messages: any[]) {
		return this.dialog.open(UpdateStatusDialogComponent, {
			data: { title, statuses, messages },
			width: '480px'
		});
	}

	showWaitingDiv() {
		const v_idWaiting = 'nemo-process-waiting-id'; // id waiting
		const v_idWaitingLoader = 'nemo-process-waiting-loader'; // id waiting
		const _show = 'nemo-show-wait';
		const _hide = 'nemo-hide-wait';
		const divWait = document.getElementById(v_idWaiting);
		const loader = document.getElementById(v_idWaitingLoader);

		if (divWait && loader) {
			if (divWait.classList.contains(_show)) {
				divWait.classList.remove(_show);
				divWait.classList.add(_hide);

				loader.classList.remove(_show);
				loader.classList.add(_hide);
			} else {
				if (divWait.classList.contains(_hide)) {
					divWait.classList.remove(_hide);
					divWait.classList.add(_show);

					loader.classList.remove(_hide);
					loader.classList.add(_show);
				} else {
					divWait.classList.remove(_show);
					divWait.classList.add(_hide);

					loader.classList.remove(_show);
					loader.classList.add(_hide);
				}
			}
		}
	}

	OffWaitingDiv() {
		const v_idWaiting = 'nemo-process-waiting-id';
		const v_idWaitingLoader = 'nemo-process-waiting-loader';
		const _show = 'nemo-show-wait';
		const _hide = 'nemo-hide-wait';
		const divWait = document.getElementById(v_idWaiting);
		const loader = document.getElementById(v_idWaitingLoader);

		if (divWait) {
			divWait.classList.remove(_show);
			divWait.classList.add(_hide);
		}
		if (loader) {
			loader.classList.remove(_show);
			loader.classList.add(_hide);
		}
	}

	setUpPaginationLabels(pagination: MatPaginator) {
		// Đã bỏ translate, để cứng tiếng Việt hoặc tiếng Anh ở đây tuỳ nhu cầu
		const trongso = 'trong tổng số'; // ví dụ hardcode hoặc truyền từ ngoài vào
		pagination._intl.firstPageLabel = 'Trang đầu';
		pagination._intl.getRangeLabel = (page: number, pageSize: number, length: number) => {
			if (length == 0 || pageSize == 0) { return `0 ${trongso} ${length}`; }

			length = Math.max(length, 0);
			const startIndex = page * pageSize;
			const endIndex = startIndex < length ? Math.min(startIndex + pageSize, length) : startIndex + pageSize;

			return `${startIndex + 1} - ${endIndex} ${trongso} ${length}`;
		};
		pagination._intl.itemsPerPageLabel = 'Số dòng trên trang';
		pagination._intl.lastPageLabel = 'Trang cuối';
		pagination._intl.nextPageLabel = 'Trang kế';
		pagination._intl.previousPageLabel = 'Trang trước';
	}
}
