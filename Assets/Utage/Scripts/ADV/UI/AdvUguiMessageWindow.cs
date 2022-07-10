﻿// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Serialization;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// メッセージウィドウ処理のサンプル
	/// </summary>
	[AddComponentMenu("Utage/ADV/AdvUguiMessageWindow")]
	public class AdvUguiMessageWindow : MonoBehaviour, IAdvMessageWindow, IAdvMessageWindowCaracterCountChecker
	{
		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine { get { return this.GetComponentCacheFindIfMissing( ref engine); } }
		[SerializeField]
		protected AdvEngine engine;

		/// <summary>既読済みのテキスト色を変えるか</summary>
		protected enum ReadColorMode
		{
			None,		//既読済みでも変えない
			Change,		//既読済みで色を変える
			ChangeIgnoreNameText,		//既読済みで色を変えるが、NameTextは変更しない
		}
		[SerializeField]
		protected ReadColorMode readColorMode = ReadColorMode.None;

		/// <summary>既読済みのテキスト色</summary>
		[SerializeField]
		protected Color readColor = new Color(0.8f, 0.8f, 0.8f);

		protected Color defaultTextColor = Color.white;
		protected Color defaultNameTextColor = Color.white;

		/// <summary>本文テキスト</summary>
		public UguiNovelText Text { get { return text; } }
		[SerializeField]
		protected UguiNovelText text=null;

		/// <summary>名前表示テキスト</summary>
		[SerializeField]
		protected Text nameText;

		/// <summary>ウインドウのルート</summary>
		[SerializeField]
		protected GameObject rootChildren;

		/// <summary>コンフィグの透明度を反映させるUIのルート</summary>
		[SerializeField,FormerlySerializedAs("transrateMessageWindowRoot")]
		protected CanvasGroup translateMessageWindowRoot;

		/// <summary>改ページ以外の入力待ちアイコン</summary>
		[SerializeField]
		protected GameObject iconWaitInput;

		/// <summary>改ページ待ちアイコン</summary>
		[SerializeField]
		protected GameObject iconBrPage;

		[SerializeField]
		protected bool isLinkPositionIconBrPage = true;

		public bool IsCurrent { get; protected set; }

		//テキストの変更処理が終わった後に呼ばれる
		public AdvMessageWindowEvent OnPostChangeText => onPostChangeText;
		[SerializeField]
		AdvMessageWindowEvent onPostChangeText = new AdvMessageWindowEvent();


		//ゲーム起動時の初期化
		public virtual void OnInit(AdvMessageWindowManager windowManager)
		{
			defaultTextColor = text.color;
			if (nameText)
			{
				defaultNameTextColor = nameText.color;
			}
			Clear();
		}

		protected virtual void Clear()
		{
			text.text = "";
			text.LengthOfView = 0;
			if (nameText) nameText.text = "";
			if (iconWaitInput) iconWaitInput.SetActive(false);
			if (iconBrPage) iconBrPage.SetActive(false);
			rootChildren.SetActive(false);
		}

		//初期状態にもどす
		public virtual void OnReset()
		{
			Clear();
		}

		//現在のウィンドウかどうかが変わった
		public virtual void OnChangeCurrent(bool isCurrent)
		{
			this.IsCurrent = isCurrent;
		}

		//アクティブ状態が変わった
		public virtual void OnChangeActive(bool isActive)
		{
			this.gameObject.SetActive(isActive);
			if (!isActive)
			{
				Clear();
			}
			else
			{
				rootChildren.SetActive(true);
			}
		}

		//テキストに変更があった場合
		public virtual void OnTextChanged(AdvMessageWindow window)
		{
			//パラメーターを反映させるために、一度クリアさせてからもう一度設定
			if (text)
			{
				text.text = "";
				text.text = window.Text.OriginalText;
				//テキストの長さを設定
				text.LengthOfView = window.TextLength;
			}

			if (nameText)
			{
				nameText.text = "";
				nameText.text = window.NameText;
			}

			switch (readColorMode)
			{
				case ReadColorMode.Change:
					text.color = Engine.Page.CheckReadPage() ? readColor : defaultTextColor;
					if (nameText)
					{
						nameText.color = Engine.Page.CheckReadPage() ? readColor : defaultNameTextColor;
					}
					break;
				case ReadColorMode.ChangeIgnoreNameText:
					text.color = Engine.Page.CheckReadPage() ? readColor : defaultTextColor;
					break;
				case ReadColorMode.None:
				default:
					break;
			}

			LinkIcon();
			OnPostChangeText.Invoke(window);
		}


		//子オブジェクトのAwakeが間に合わないと、
		//イベントリストナーが登録されないのでいったんここでアクティブ状態にする
		protected virtual void Awake()
		{
			if (!this.rootChildren.activeSelf)
			{
				rootChildren.SetActive(true);
				rootChildren.SetActive(false);
			}
		}

		//毎フレームの更新
		protected virtual void LateUpdate()
		{
			if (Engine.UiManager.Status == AdvUiManager.UiStatus.Default)
			{
				rootChildren.SetActive(Engine.UiManager.IsShowingMessageWindow);
				if (Engine.UiManager.IsShowingMessageWindow)
				{
					//ウィンドのアルファ値反映
					if (translateMessageWindowRoot!=null)
					{
						translateMessageWindowRoot.alpha = Engine.Config.MessageWindowAlpha;
					}
				}
			}

			UpdateCurrent();
		}

		//現在のメッセージウィンドウの場合のみの更新
		protected virtual void UpdateCurrent()
		{
			if (!IsCurrent) return;

			if (Engine.UiManager.Status == AdvUiManager.UiStatus.Default)
			{
				if (Engine.UiManager.IsShowingMessageWindow)
				{
					//テキストの文字送り
					text.LengthOfView = Engine.Page.CurrentTextLength;
				}
				LinkIcon();
			}
		}

		//アイコンの場所をテキストの末端にあわせる
		protected virtual void LinkIcon()
		{
			if (iconWaitInput == null)
			{
				//ページ途中の入力待ちアイコンが設定されてない場合(古いバージョン）対応
				//改ページ待ちと入力待ちを同じ扱い
				LinkIconSub(iconBrPage, Engine.Page.IsWaitInputInPage || Engine.Page.IsWaitBrPage);
			}
			else
			{
				//入力待ち
				LinkIconSub(iconWaitInput, Engine.Page.IsWaitInputInPage);
				//改ページ待ち
				LinkIconSub(iconBrPage, Engine.Page.IsWaitBrPage);
			}
		}

		//アイコンの場所をテキストの末端にあわせる
		protected virtual void LinkIconSub(GameObject icon, bool isActive)
		{
			if (icon == null) return;

			if (!Engine.UiManager.IsShowingMessageWindow)
			{
				icon.SetActive(false);
			}
			else
			{
				icon.SetActive(isActive);
				if (isLinkPositionIconBrPage) icon.transform.localPosition = text.CurrentEndPosition;
			}
		}

		//ウインドウ閉じるボタンが押された
		public virtual void OnTapCloseWindow()
		{
			Engine.UiManager.Status = AdvUiManager.UiStatus.HideMessageWindow;
		}

		//バックログボタンが押された
		public virtual void OnTapBackLog()
		{
			Engine.UiManager.Status = AdvUiManager.UiStatus.Backlog;
		}

		//表示文字数チェック開始（今設定されているテキストを返す）
		public virtual string StartCheckCaracterCount()
		{
			if (text==null)
			{
				return "";
			}
			return text.text;
		}

		//指定テキストに対する表示文字数チェック
		public virtual bool TryCheckCaracterCount(string text, out int count, out string errorString)
		{
			return this.text.TextGenerator.EditorCheckRect(text, out count, out errorString);
		}

		//Startで設定されていたテキストに戻す
		public virtual void EndCheckCaracterCount(string text)
		{
			if (this.text == null)
			{
				return;
			}
			this.text.text = text;
		}
	}

}
