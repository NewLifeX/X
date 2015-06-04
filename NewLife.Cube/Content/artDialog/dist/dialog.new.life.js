(function ($, dialog) {

	var dm = document.documentMode;
	var ie = !-[1, ];
	var ie6 = ie && !window.XMLHttpRequest;
	var ie7 = ie && dm == 7;
	var ie8 = ie && dm == 8;
	var ie9 = dm;

	window.infoDialog = window.errorDialog = function (title, msg, modal, jumpUrl) {

		var len = arguments.length;
		if (len == 1) {
			title = '';
			msg = title;
			modal = true;
			jumpUrl = null;
		} else if (len == 3) {
			if ($.type(modal) == 'string') {
				jumpUrl = modal;
				modal = true;
			} else if ($.type(modal) == 'boolean' || $.type(modal) == 'number') {
				modal = !!modal;
				jumpUrl = null;
			}
		} else if (len == 4) {
			modal = !!modal;
			//jumpUrl = jumpUrl;
		}

		var d = dialog.get('global_error_dialog') || dialog({
			id: 'global_error_dialog',
			title: title || '提示',
			content: '',
			backdropBackground: '#fff',
			backdropOpacity: 0.4,
			okValue: '确定',
			cancelDisplay: false,
			cancel: function () {
				this.ok();
			},
			ok: function () {

				if (jumpUrl) {
					if (jumpUrl == 'close') {
						window.opener && window.opener.setTimeout('location.reload()', 800);
						(ie6 || dm) && window.open('', '_self', '');
						window.close();
					} else if ($.type(jumpUrl) == 'function') {

					    if (jumpUrl() === false) {
					        //回调返回flase阻止关闭dialog
					        return false;
					    }
					} else {
					    if (jumpUrl.indexOf('iframe') > -1) {
					        var url = jumpUrl.substr(6).split('|'),
								fid = url[0],
								url = url[1];
					        console.log(fid)
					        var iframe = $(fid || 'iframe');
					        if (iframe.length) {
					            iframe.get('0').src = url;
					        }
					    }
					    else {
					        window.location.href = jumpUrl;
					    }
					}
				}
				this.close().remove();
			}
		});
		if ($.type(msg) == 'object') {
			$.map(msg, function (v, k) {
				if (typeof d[k] == 'function') {
					d[k](v || '');
				}
			});
		} else {
			d.content(msg || '');
		}

		d[modal ? 'showModal' : 'show']();
		return d;
	}

	window.confirmDialog = function (msg, title, callback) {
		 
		var len = arguments.length;
		if (len == 2) {
			if ($.type(title) == 'function') {
				callback = title;
				title = '';
			}
		}

		var d = dialog.get('global_confirm_dialog') || dialog({
			id: 'global_confirm_dialog',
			title: title || '提示',
			content: '',
			backdropBackground: '#fff',
			backdropOpacity: 0.4,
			okValue: '确定',
			cancelValue:'取消',
			cancelDisplay: true,
			cancel: function () {
				this.close().remove();
			},
			ok: function () { 
				callback && callback();
				this.close().remove();
			}
		});
		if ($.type(msg) == 'object') {
			$.map(msg, function (v, k) {
				if (typeof d[k] == 'function') {
					d[k](v || '');
				}
			});
		} else {
			d.content(msg || '');
		}
		d.showModal();

		return d;
	};

	window.tips = function (msg, modal, time, jumpUrl) {
		var len = arguments.length;
		if (len == 1) {
			modal = false;
			time = 2000;
		} else if (len == 2) {
			if ($.type(modal) == 'number' && modal >= 1000) {
				time = modal;
				modal = false;
			} else if ($.type(modal) == 'boolean') {
				modal = !!modal;
				time = 2000;
			}
		} else if (len == 3) {
			modal = !!modal;
			time = +time >= 1000 ? time : 0;
		}

		var d = dialog.get('global_tips_dialog') || dialog({
			id: 'global_tips_dialog',
			backdropBackground: '#fff',
			backdropOpacity: 0.4,
			content: ''
		});

		if ($.type(msg) == 'boolean' || msg == '') {
			d.close().remove();
			return d;
		}

		if ($.type(msg) == 'object') {
			$.map(msg, function (v, k) {
				if (typeof d[k] == 'function') {
					d[k](v || '');
				}
			});
		} else {
			d.content(msg || 'tips');
		}

		d[modal ? 'showModal' : 'show']();

		if (time > 0) {
		    setTimeout(function () {
		        
				if (jumpUrl) {
					if (jumpUrl == 'close') {
						window.opener && window.opener.setTimeout('location.reload()', 800);
						(ie6 || dm) && window.open('', '_self', '');
						window.close();
					} else if ($.type(jumpUrl) == 'function') {

						if (jumpUrl() === false) {
							//回调返回flase阻止关闭dialog
							return false;
						}
					} else {
						if (jumpUrl.indexOf('iframe') > -1) {
							var url = jumpUrl.substr(6).split('|'),
								fid = url[0] ,
								url = url[1];
							console.log(fid)
							var iframe = $( fid || 'iframe');
							if (iframe.length) {
								iframe.get('0').src = url;
							}
						}
						 else {
							window.location.href = jumpUrl;
						}
					}
						
				}
				d.close().remove();
			}, time);
		}

		return d;
	}


})(jQuery, dialog);
