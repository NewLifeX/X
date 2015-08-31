using System.Reflection;
using System.Runtime.InteropServices;
using System.Web.UI;

// 鏈夊叧绋嬪簭闆嗙殑甯歌淇℃伅閫氳繃涓嬪垪灞炴€ч泦
// 鎺у埗銆傛洿鏀硅繖浜涘睘鎬у€煎彲淇敼
// 涓庣▼搴忛泦鍏宠仈鐨勪俊鎭€?
[assembly: AssemblyTitle("鏂扮敓鍛芥帶浠跺簱")]
[assembly: AssemblyDescription("甯哥敤鎺т欢")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyProduct("XControl")]
[assembly: AssemblyCulture("")]

// 灏?ComVisible 璁剧疆涓?false 浣挎绋嬪簭闆嗕腑鐨勭被鍨?
// 瀵?COM 缁勪欢涓嶅彲瑙併€傚鏋滈渶瑕佷粠 COM 璁块棶姝ょ▼搴忛泦涓殑绫诲瀷锛?
// 鍒欏皢璇ョ被鍨嬩笂鐨?ComVisible 灞炴€ц缃负 true銆?
[assembly: ComVisible(false)]

// 濡傛灉姝ら」鐩悜 COM 鍏紑锛屽垯涓嬪垪 GUID 鐢ㄤ簬绫诲瀷搴撶殑 ID
[assembly: Guid("fa826606-eee8-4b30-a41f-5e37420328a6")]

// 璁剧疆鎺т欢鍓嶇紑
[assembly: TagPrefix("XControl", "XCL")]

// 鐗瑰埆瑕佹敞鎰忥紝杩欓噷寰楀姞涓婇粯璁ゅ懡鍚嶇┖闂村拰鐩綍鍚嶏紝鍥犱负vs2005缂栬瘧鐨勬椂鍊欎細缁檍s鏂囦欢鍔犱笂杩欎簺涓滀笢鐨?
[assembly: WebResource("XControl.TextBox.Validator.js", "application/x-javascript")]

// 绋嬪簭闆嗙殑鐗堟湰淇℃伅鐢变笅闈㈠洓涓€肩粍鎴?
//
//      涓荤増鏈?
//      娆＄増鏈?
//      鍐呴儴鐗堟湰鍙?
//      淇鍙?
//
// 鍙互鎸囧畾鎵€鏈夎繖浜涘€硷紝涔熷彲浠ヤ娇鐢ㄢ€滀慨璁㈠彿鈥濆拰鈥滃唴閮ㄧ増鏈彿鈥濈殑榛樿鍊硷紝
// 鏂规硶鏄寜濡備笅鎵€绀轰娇鐢ㄢ€?鈥?
[assembly: AssemblyVersion("1.14.*")]
[assembly: AssemblyFileVersion("1.14.2014.0214")]

/*
 * v1.14.2014.0214  GridViewExtender鎺у埗ObjectDataSource锛屾煡璇㈢粨鏋滀笉瓒虫渶澶ф暟锛屼笖浠?寮€濮嬶紝鍒欎笉闇€瑕佸啀娆℃煡璇㈡€昏褰曟暟
 * 
 * v1.13.2012.1225  GridViewExtender鎷︽埅GridView璋冪敤ObjectDataSource鐨処nsert/Update/Delete寮傚父锛屽苟閫氳繃alert鍚戠敤鎴峰弸濂芥彁绀?
 * 
 * v1.13.2012.0401  淇敼鏁堥獙鐮佹帶浠禫erifyCodeBox,浣挎洿绗﹀悎浼犵粺asp.net楠岃瘉鎺т欢鐨勪娇鐢ㄤ範鎯?淇浜嗛獙璇佺爜鍥剧墖鍙兘闅愯棌鐨勯棶棰?浣跨敤鏂规硶娌℃湁鍙樺寲
 * 
 * v1.13.2012.0218  GridViewExtender澧炲姞DataSource鍜孯owCount锛屾柟渚胯闂甇bjectDataSource杩斿洖鐨勬暟鎹?
 * 
 * v1.13.2011.1123  鏇存柊My97 DatePicker鐨勭増鏈埌4.72,浠ュ強瀵瑰簲鐨刟sp.net鏈嶅姟绔帶浠跺皝瑁?浼氳В鍐矷E9涓?鍦ㄨ繍琛屾椂鍒涘缓鐨刬frame涓彧鑳戒娇鐢ㄤ竴娆＄殑闂
 *                  澧炲姞MenuField,鍦℅ridView涓娇鐢ㄧ殑鏁版嵁缁戝畾瀛楁,鍙樉绀鸿彍鍗曟牱寮忕殑瀛楁
 * 
 * v1.12.2011.1117  GridViewExtender澧炲姞涓€涓姛鑳斤紝鏀寔鐩爣GridView缁戝畾鐨凮bjectDataSource浣跨敤鎺掑簭鐨勯粯璁ゅ€?
 * 
 * v1.11.2011.1107  淇LinkBoxField涓病鏈夋敞鍐孏ridViewExtender.js鐨勯棶棰?
 * 
 * v1.10.2011.0727  淇寮圭獥缁勪欢鍦ㄧ浜屾鎵撳紑鏃秡-index鏄剧ず閿欒鐨刡ug
 * 
 * v1.10.2011.0628  缁欐瘡涓€涓狟ox鎺т欢鍔犱笂榛樿鍊煎睘鎬х殑鐗规€э紝浠ヤ究浜庤嚜瀹氫箟琛ㄥ崟澶勭悊
 * 
 * v1.9.2011.0530   GridViewExtender鎺т欢锛屽鍔犲弻鍑绘椂鎵ц缂栬緫鎿嶄綔鐨勫姛鑳?
 * 
 * v1.9.2011.0525   淇鏁板瓧 娴偣鏁拌緭鍏ユ帶浠剁殑涓€浜涚粏鑺傞棶棰?浣垮湪firefox涓嬪彲姝ｅ父杩愯
 * 
 * v1.9.2011.0224   +鎵╁睍鎺т欢鍩虹被锛屼慨鏀笹etPropertyValue鏂规硶锛屽綋娌℃湁璁惧畾鐨刅iewState鏃讹紝浠庡叏灞€Appconfig涓鍙?
 * 
 * v1.8.2011.0116   Add锛欸ridView鎵╁睍鎺т欢澧炲姞鍒嗛〉妯＄増銆佸閫?
 * 
 * v1.7.2010.1015   澧炲姞瀵硅瘽妗嗘帶浠?
 * 
 * v1.6.2010.0830   淇鏃堕棿鏃ユ湡閫夋嫨鎺т欢鍥炲彂鍚庡け鏁堢殑闂
 * 
 * v1.5.2010.0706   澧炲姞閫夋嫨杈撳叆鎺т欢ChooseButton锛岀敤浜庡鐞嗗鏉傞€夋嫨
 * 
 * v1.4.2010.0702   閲嶅啓DropDownList澶勭悊寮傚父椤圭殑閫昏緫
 * 
 * v1.3.2010.0625   淇DropDownList涓病鏈夎繃婊ら噸澶嶅紓甯搁」鐨勯棶棰?
 * 
 * v1.2.2010.0621   澧炲姞DataPager鍒嗛〉鎺т欢锛屽墺绂昏嚜GridView
 * 
 * v1.1.2010.0604   澧炲姞DropDownList鍜孋heckBoxList锛屼慨姝ｅ叧鑱斿弬鏁癘DS鏃朵袱娆＄粦瀹氱殑BUG
 *                  澧炲姞DateTimePicker锛屽己澶х殑鏃ユ湡鏃堕棿鎺т欢
 *                  
 * v1.0.2008.1212   鍒涘缓鎺т欢搴?
 *
**/