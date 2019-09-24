# LiveTalkZinraiFAQSample
LiveTalk常時ファイル出力で出力したテキストを、Zinrai FAQ検索 をFAQを検索し、その結果をLiveTalk画面に表示するサンプルです。  
本サンプルコードは、.NET Core 3.0で作成しています。コードレベルでは.NET Framework 4.6と互換性があります。

![Process](https://github.com/FujitsuSSL-LiveTalk/LiveTalkZinraiFAQSample/blob/images/README.png)

# サンプルコードの動き
本サンプルでは、「Zinraiさん：」と呼び掛けて発話した行をZirani FAQ検索に渡して結果を表示します。サンプルコード動作を簡単に説明すると次のような動作をします。  
1. LiveTalkで音声認識した結果がファイルに出力されるので、それを自動的に読込み、「Zinraiさん：」から始まる行であれば、Zirani FAQ検索 APIを呼び出します。
2. Zirani FAQ検索から戻ってきたテキストをLiveTalkが常時ファイル入力として監視しているファイルに出力します。
3. LiveTalkが常時ファイル出力に出力されたFAQ検索結果を画面に表示します。このときユーザー名は該当ファイル名となります。
※ LiveTalk連携した他のLiveTalk端末からの発話も対象となり、結果もLiveTalk連携しているすべての端末に表示されます。


# 事前準備
1. [Zinrai FAQ検索 API](https://www.fujitsu.com/jp/solutions/business-technology/ai/ai-zinrai/services/platform/faq-search/index.html)の申込を行い、Zinrai FAQ検索 APIが有効なclient_idとclient_secretを入手します。
2. client_idとclient_secretをサンプルコードに指定します。
3. インターネットとの接続がPROXY経由の場合、PROXYサーバーや認証情報を設定してください。
4. LiveTalkで、デスクトップに常時ファイル出力を「LiveTalkOutput.csv」、常時ファイル入力を「Zirai.txt」として指定してください。


# 連絡事項
本ソースコードは、LiveTalkの保守サポート範囲に含まれません。  
頂いたissueについては、必ずしも返信できない場合があります。  
LiveTalkそのものに関するご質問は、公式WEBサイトのお問い合わせ窓口からご連絡ください。
