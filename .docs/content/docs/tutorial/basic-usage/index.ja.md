---
title: 基本的な使い方
---

基本的な使い方
===

自動最適化を使用する {#trace-and-optimize}
---

アバターには自動的に行える最適化がいくつかあります。

- 使用していないBlendShape(シェイプキー)[^blend-shape]の除去
  - BlendShapeの値が0以外のときは処理負荷が発生するため、値をアニメーション等で変更しないBlendShapeは固定すると負荷が軽くなります。
  - 値が常に0である場合でも、固定することでアバターの容量を削減することができます。
- 使われていないPhysBone等の削除
  - 揺らす対象として存在するメッシュ[^mesh]が常に無効になっているPhysBoneなどのように、揺らす必要のないPhysBoneが有効になっている場合は計算負荷が余分に発生してしまいます。
- アニメーションしたりPhysBoneで揺らしたりすることのないボーンの統合
  - 服のボーンを素体のボーンに入れ子状にして着せるような場合には、それ自身を動かすことがないボーンが多く発生します。そのようなボーンは余分な負荷を発生させてしまいます。
- 一緒に切り替えていたり、切り替えることがなかったりするメッシュ同士の統合
  - アバターに服が1着しかない場合、体、髪、服などを別々のメッシュにしておく必要はないかもしれません。
  - 切り替え可能な複数の服がある場合でも、体、髪、下着などを別々のメッシュにしておく必要はないかもしれません。

AvatarOptimizerでは、アバターのルートに`AAO Trace And Optimize`コンポーネントを追加するだけで、これらの最適化を自動で行うことができます！

![add-trace-and-optimize.png](add-trace-and-optimize.png)

[^blend-shape]: BlendShapeはUnity上のモーフィングの名前です。MayaではTarget Shape、BlenderではShape Key、MetasequoiaやMMDではモーフと呼ばれます。
[^mesh]: この記事でのメッシュは、UnityのMesh assetではなく、SkinnedMeshRendererやMeshRendererを意味しています。

アバターをアップロードする {#upload-avatar}
---

`AAO Trace and Optimize`コンポーネントを付けた状態で、試しにアバターをアップロードしてみましょう！
AAO: Avatar Optimizerは非破壊改変ツールであり、Playモードに入るときかアバターをビルドするときに処理が行われるため、アップロードを行うのに特別な手順は必要ありません。
通常と同じように、VRCSDKのControl Panelからアバターをアップロードしてください。

ただし、Android(Quest)向けアップロードを行う場合などにおいて、Avatar Optimizerの最適化等によって制限の範囲内に収まるにも関わらず、 VRCSDKのビルド前チェックの時点で制限を超過していてアップロードボタンが押せない場合があります。\
ビルド前チェックをスキップする方法はいくつかあります。詳しくは[よくある質問][skip-hard-limit-faq]を参照してください。

[skip-hard-limit-faq]: ../../faq/#i-cannot-upload-the-avatar-because-of-pre-build-hard-limit-check

{{< hint info >}}

### UploadせずにPerformance Rankを見る方法 {#performance-rank-without-upload}

非破壊改変ツールを使用した改変では、VRCSDKのControl Panel上のPerformance Rankはあてにならなくなります。

その代わりとして、Playモードに入った際のPerformance Rankをanatawa12's Gist PackのActual Performance Windowを使用して確認することができます。
詳しくは[anatawa12's Gist Packの使い方][gists-basic-usage]および[Actual Performance Windowのドキュメント][Actual Performance Window]を参照してください。

[gists-basic-usage]: https://vpm.anatawa12.com/gists/ja/docs/basic-usage/
[Actual Performance Window]: https://vpm.anatawa12.com/gists/ja/docs/reference/actual-performance-window/

{{< /hint >}}

{{< hint info >}}

### 非破壊改変ツールを手動で適用する方法 {#how-to-manual-bake}

アバターのGameObjectを右クリックして出てくるメニューの`NDM Framework`から`Manual bake avatar`をクリックすると、非破壊ツールによる処理を手動で適用することができます。

`Manual bake avatar`は初めにアバターを複製し、その複製に対して非破壊ツールの処理を適用させるため、元のアバターは変更されないままになります。

VRChat向けアバターをVRM形式で出力したい場合などにご活用ください。

{{< /hint >}}

貫通防止用BlendShapeを利用してポリゴンを減らす {#remove-mesh-by-blendshape}
---

服で隠れていたりして見えないような部分のメッシュを削除すると、見た目に影響させずに描画負荷やBlendShapeの処理負荷などを減らして軽量化することができます。
これを簡単に実現するために、AvatarOptimizerでは多くの素体に含まれている貫通防止用のBlendShapeを利用してメッシュを削除することができます！

素体のメッシュに`AAO Remove Mesh By BlendShape`コンポーネントを追加しましょう！

そして削除したい部位の貫通防止用BlendShapeを下の一覧から選択しましょう！

消えて欲しい箇所が消えない場合や、消えて欲しくない箇所が消えてしまう場合には、`許容差`の値を調整する必要があることがあります！
`許容差`は、頂点がBlendShapeによってどのぐらい動けば削除するかを決定するものです。
消えてほしい箇所が消えない場合は値を少し大きく、思っていない箇所が消えてしまう場合は値を少し小さくしましょう！

![remove mesh by BlendShape](./remove-mesh-by-blendshape.png)

<!--

メッシュを統合してMesh Renderersを減らす {#merge-skinned-mesh}
---

Avatar Optimizerを使用すると簡単にSkinned Meshを統合することができます！
Skinned Meshを統合すると個別にオン・オフできなくなりますが、統合することで軽量化に繋がります！

{{< hint info >}}

**なぜSkinned Meshを統合するの？**

Skinned Meshを統合することでメッシュを変形させる処理の回数が減り、負荷が軽くなります。
また、MergeSkinnedMeshでは、同じマテリアルを使用しているマテリアルスロットも統合することができるので、描画負荷も減らす事ができます。

{{< /hint >}}

一番単純なパターンとして、Anonちゃんを軽量化してみます。

![start.png](./start.png)

まず初めに、統合先のGameObjectを作りましょう。
アバターのGameObjectを右クリックから `Create Empty` をクリックして新たなGameObjectを作ります。
そうしたら、わかりやすい名前に変えておいてください。この記事では`Anon_Merged`とします。

![create-empty.png](./create-empty.png)

次に、`Anon_Merged`に`AAO Merge Skinned Mesh`コンポーネントを追加しましょう。

![add-merge-skinned-mesh.png](./add-merge-skinned-mesh.png)

すると`AAO Merge Skinned Mesh`コンポーネントと`Skinned Mesh Renderer`コンポーネントが追加されます。
後者が統合先のメッシュになります。

統合したいメッシュを`AAO Merge Skinned Mesh`コンポーネントに楽に指定するために、`Anon_Merged`を選択した状態でinspectorをロックしましょう。
こうすることで複数のメッシュをまとめてドラックアンドドロップできるようになります。[^tip-lock-inspector]

![lock-inspector.png](./lock-inspector.png)

それでは、顔のメッシュであるBody以外のメッシュをHierarchyで選択し、`AAO Merge Skinned Mesh`コンポーネント内のSkinned Renderersにドラックアンドドロップで指定しましょう！

![drag-and-drop.png](./drag-and-drop.png)

{{< hint info >}}

**なせ顔のメッシュは統合しないの？**

BlendShape(シェイプキー)は頂点数とBlendShape数の積に比例して重くなる処理です。
そのため、BlendShapeの数が多い顔のメッシュを頂点数の多い体のメッシュと統合するとかえって重くなってしまうため、顔は別のままにするのを推奨しています。

{{< /hint >}}

続いて、`Anon_Merged`の設定をしましょう！

`AAO Merge Skinned Mesh`コンポーネントは諸事情[^merge-skinned-mesh]により、ボーン、メッシュ、マテリアル、BlendShape、Bounds以外の設定を自動的には行いません。
そのため、統合先のメッシュ(`AAO Merge Skinned Mesh`コンポーネントと同時に追加された`Skinned Mesh Renderer`コンポーネント)にある`Anchor Override`, `Root Bone`等の項目には別途手動で設定が必要です。
`Anchor Override`には素体(Body等)で設定されているものを、`Root Bone`には`Hips`を指定すると上手くいくことが多いと思います。

[^tip-lock-inspector]: PhysBoneに複数のコライダーを指定したりするのにも使えます。色んなところで使えるので覚えておくと便利だと思います。
[^merge-skinned-mesh]: Root Bone/Anchor Overrideは等しくないと統合できないため対応予定がありません。もし良いアルゴリズムがあれば教えてください。

-->
