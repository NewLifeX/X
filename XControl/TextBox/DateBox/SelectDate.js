

// 日期选择框

// @文本框对象

// @年

// @月

function SelectDate(obj, inYr, inMon){
	var yr=new Date().getFullYear();
	var mon=new Date().getMonth()+1;
	if(ConvertToDate(obj.value)!='NaN'){
		yr=ConvertToDate(obj.value).getFullYear();
		mon=ConvertToDate(obj.value).getMonth()+1;
	}
	if(inYr&&inMon){
		yr=inYr;
		mon=inMon;
	}
	//准备弹出Popup
	var p=window.createPopup();
	var b=p.document.body;
	b.innerHTML=Calendar(yr, mon);
	//Popup完全弹出后，选择今年的这个月。可在HTML处理，js不处理

	//b.onload=function(){b.all.yr.selectedIndex=5;b.all.mon.selectedIndex=mon-1;}
	//选择的年月改变后，重新执行本函数，显示新的年月选择。考虑用this代替这里的SelectDate
	b.all.yr.onchange=function(){SelectDate(obj,this.options[this.selectedIndex].value,mon);}
	b.all.mon.onchange=function(){SelectDate(obj,yr,this.options[this.selectedIndex].value);}
	//设置本月日期，下月日期的颜色
	for(var i=0;i<b.all.length;i++){
		if(b.all[i].setV){
			if(b.all[i].dt==obj.value){
				b.all[i].style.border = "1px solid #000000";
				b.all[i].style.backgroundColor = "#EEEEEE";
				b.all[i].style.cursor='default';
			}
			else{
				//点击后设置文本框的值。如果文本框的值并不等于现在选择的日期，则刷新Popup
				b.all[i].onclick=function(){var ov=obj.value;obj.value=this.dt;if(this.dt!=ov&&obj.onchange){obj.onchange();}p.hide();}
				//鼠标经过时，显示手指，离开时显示默认指针

				b.all[i].onmouseover=function(){this.style.cursor='hand';}
				b.all[i].onmouseout=function(){this.style.cursor='default';}
			}
		}
	}
	p.show(0,20,220,200,obj);
}

// 根据年月构造日历

function Calendar(yr, mon){
	var html='';
	var today=new Date();
	var fd=new Date(yr,mon-1,1);
	var d=1-fd.getDay();
	var cd;
	html='<table border="0" cellspacing="0" cellpadding="0" width="100%" height="100%" style="font-size: 12px;text-align: center;border: 1px outset #999999;">';
	html+='<tr style="background-color:#CCCCCC;cursor: default;">';
	html+='<td colspan="7">';
	html+=Calendar_yr(yr)+' 年 ';
	html+=Calendar_mon(mon)+' 月';
	html+='</td>';
	html+='</tr>';
	html+='<tr style="background-color:#999999;color:#FFFFFF;height:20px;cursor: default;">';
	html+='<td>日</td><td>一</td><td>二</td><td>三</td><td>四</td><td>五</td><td>六</td>';
	html+='</tr>';
	for(var i=0;i<6;i++){
		html+='<tr>';
		for(var j=0;j<7;j++){
			cd=new Date(yr,mon-1,d++);
			html+='<td setV="yes" style="cursor: hand;" dt="'+FormatDate(cd)+'">';
			if(cd.getMonth()!=mon-1)
				html+=cd.getDate().toString().fontcolor('#CCCCCC');
			else if(cd.getMonth()==today.getMonth()&&cd.getDate()==today.getDate())
				html+=cd.getDate().toString().bold();
			else
				html+=cd.getDate();
			html+='</td>';
		}
		html+='</tr>';
	}
	html+='<tr>';
	html+='<td colspan="7" setV="yes" style="cursor: hand;" dt="'+FormatDate(today)+'">';
	html+='今天:'+FormatDate(today);
	html+='</td>';
	html+='</tr>';
	html+='</table>';
	return html;
}

// 年选择，前后5年

function Calendar_yr(inYr){
	var yr=new Number(inYr);
	var html='<select id="yr">';
	// 默认选择今年
	for(var i=yr-5;i<=yr+5;i++)
		html+='<option value="'+i+'"'+(i==yr?' Selected':'')+'>'+i+'</option>';
	html+='</select>';
	return html;
}

// 月选择，1到12月

function Calendar_mon(inMon){
	var mon=new Number(inMon);
	var html='<select id="mon">';
	// 默认选择本月
	for(var i=1;i<=12;i++)html+='<option value="'+i+'"'+(i==mon?' Selected':'')+'>'+i+'</option>';
	html+='</select>';
	return html;
}

// 格式化日期为字符串

function FormatDate(date){
	return date.getFullYear()+'-'+(date.getMonth()+1)+'-'+date.getDate();
}

function ConvertToDate(str){
	var arr=str.split('-');
	if(arr.length!=3)return 'NaN';
	return new Date(arr[0],arr[1]-1,arr[2]);
}
