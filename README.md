# TSKT_UI

+ Unity Version : 2019.1.10

# Install

+ TextMeshPro（2.0.1）をインポートする
    + PackageManagerを使ってインポート
    + メニューのWindow->TextMeshPro->Import TMP Essential Resources
+ Unity_TSKT_UIとUnity_TSKT_Containerをインポートする
    + manifest.jsonに↓を追記すれば良い

Packages/manifest.json
```json
{
  "dependencies": {
    "com.github.enue.tskt_ui": "https://github.com/enue/Unity_TSKT_UI.git",
    "com.github.enue.tskt_container": "https://github.com/enue/Unity_TSKT_Container.git",
  }
}
```

# 機能

## Textにルビを表示する

+ 本文用の`Text`コンポーネントを作成する。
+ ルビ用の`Text`コンポーネントを作成し、`RubyText`をアタッチする。`RubyText`は↑の本文用オブジェクトを参照しておく。
+ 二つのオブジェクトのrectは一致させておく。（ルビと本文は親子関係ににしとくのが自然でしょう）
+ コード実行

```cs
[SerializeField]
RubyText ruby;

[SerializeField]
Text body;

void Start()
{
    var message = "{吾輩:わがはい}は猫である。<color=red>名前はまだ無い</color>。\nどこで生れたかとんと{見<color=red>当:けんとう}がつか</color>ぬ。何でも薄暗いじめじめした所で<color=red>ニャーニャー</color>泣いていた事だけは記憶している。";
    var stringWithRuby = StringWithRuby.Parse(message);
    ruby.Set(stringWithRuby);
    body.text = stringWithRuby.body;
}
```

### テキストを1文字ずつ表示する

```cs
IEnumerator Start()
{
    var message = "{吾輩:わがはい}は猫である。<color=red>名前はまだ無い</color>。\nどこで生れたかとんと{見<color=red>当:けんとう}がつか</color>ぬ。何でも薄暗いじめじめした所で<color=red>ニャーニャー</color>泣いていた事だけは記憶している。";
    var stringWithRuby = StringWithRuby.Parse(message)
        .FoldTag()
        .WrapWithHyphenation(body, new HyphenationJpns.Ruler());

    for (int i = 0; i < stringWithRuby.body.Length; ++i)
    {
        if (stringWithRuby.body[i] == '\n')
        {
            continue;
        }
        var sub = stringWithRuby.Substring(0, i).UnfoldTag();
        ruby.Set(sub);
        body.text = sub.body;

        yield return new WaitForSeconds(0.05f);
    }
}
```

## TextMeshProにルビを表示する

+ 本文用の`TextMeshPro`もしくは`TextMeshProUGUI`コンポーネントを作成する。
+ ルビ用の`TextMeshPro`もしくは`TextMeshProUGUI`コンポーネントを作成し、`TMP_RubyText`をアタッチする。`TMP_RubyText`は↑の本文用オブジェクトを参照しておく。
+ 二つのオブジェクトのrectは一致させておく。（ルビと本文は親子関係ににしとくのが自然でしょう）
+ コード実行

```cs
[SerializeField]
TMP_RubyText ruby;

[SerializeField]
TMPro.TMP_Text body;

void Start()
{
    var message = "{吾輩:わがはい}は猫である。<color=red>名前はまだ無い</color>。\nどこで生れたかとんと{見<color=red>当:けんとう}がつか</color>ぬ。何でも薄暗いじめじめした所で<color=red>ニャーニャー</color>泣いていた事だけは記憶している。";
    var stringWithRuby = StringWithRuby.Parse(message);
    ruby.Set(stringWithRuby);
    body.text = stringWithRuby.body;
}
```

### テキストを1文字ずつ表示する

```cs
IEnumerator Start()
{
    var message = "{吾輩:わがはい}は猫である。<color=red>名前はまだ無い</color>。\nどこで生れたかとんと{見<color=red>当:けんとう}がつか</color>ぬ。何でも薄暗いじめじめした所で<color=red>ニャーニャー</color>泣いていた事だけは記憶している。";
    var stringWithRuby = StringWithRuby.Parse(message).FoldTag();

    for (int i = 0; i < stringWithRuby.body.Length; ++i)
    {
        var sub = stringWithRuby.Substring(0, i).UnfoldTag();
        ruby.Set(sub);
        body.text = sub.body;

        yield return new WaitForSeconds(0.05f);
    }
}
```


