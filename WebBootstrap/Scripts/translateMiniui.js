NewLifeMiniui={
	components:{},
	uids:{},
	isReady:false,
	byId:function (object) {
		if(typeof object=='string'){
			if(object.charAt(0)=='#'){
				object=object.substr(1);
			}
			return document.getElementById(object);
		} else
			return object;
	}
}