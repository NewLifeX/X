var langList =
[
	{ name: 'en', charset: 'UTF-8', script: '<%= WebResource("XControl.TextBox.DateTimePicker.lang.en.js") %>' },
	{ name: 'zh-cn', charset: 'UTF-8', script: '<%= WebResource("XControl.TextBox.DateTimePicker.lang.zh-cn.js") %>' },
	{ name: 'zh-tw', charset: 'UTF-8', script: '<%= WebResource("XControl.TextBox.DateTimePicker.lang.zh-tw.js") %>' }
];

var skinList =
[
	{ name: 'default', charset: 'UTF-8', css: '<%= WebResource("XControl.TextBox.DateTimePicker.skin.default.datepicker.css") %>' }
	, { name: 'whyGreen', charset: 'UTF-8', css: '<%= WebResource("XControl.TextBox.DateTimePicker.skin.whyGreen.datepicker.css") %>' }
/*注释不需要的皮肤可以减少网络请求
*/
];