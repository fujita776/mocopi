using UnityEngine;
using UnityEditor;

// GameView���t���X�N���[���ŕ\������X�N���v�g(Windows��F11�AmacOS��Command+Shift+F�Ńg�O������)
public class FullScreenGameView
{
//    const string menuPath = "Window/Game (Full Screen)";

//#if UNITY_EDITOR_WIN
//    [MenuItem(menuPath + " _F11", false, 2001)]
//#elif UNITY_EDITOR_OSX
//    [MenuItem(menuPath + " %#f", false, 2001)]
//#endif
//    public static void Execute()
//    {
//        EditorWindow gameView = GetGameView();

//        if (Menu.GetChecked(menuPath) == false)
//        {
//            gameView.Close();       // �h�b�L���O���ɃT�C�Y��ς����Editor�̃T�C�Y���ς���Ă��܂����߈�U����

//            float width = 1000f;
//            float height = 500f;
//            float offset = 17.0f;   // GameView�̃R���g���[���o�[�̍���(Unity2017.1�̏ꍇ) ���^�u��g�͌v�Z�ɓ���Ȃ�

//            gameView = GetGameView();
//            gameView.minSize = new Vector2(width, height + offset);
//            gameView.position = new Rect(0, -offset, width, height + offset);

//            Menu.SetChecked(menuPath, true);
//        }
//        else
//        {
//            // �ʒu�p�����[�^���f�t�H���g�ɖ߂��Ă���Close
//            gameView.minSize = minSize;
//            gameView.position = position;
//            gameView.Close();

//            Menu.SetChecked(menuPath, false);
//        }
//    }

//    private static EditorWindow GetGameView()
//    {
//        // �E�B���h�E�����݂��Ȃ��ꍇ�͐��������
//        return EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor"));
//    }

//    // �f�t�H���g�ʒu�p�����[�^(���̈ʒu�ɂ͖߂��Ȃ��̂ŁA�����₷���ʒu���T�C�Y�ɂ��Ă���)
//    private static Vector2 minSize = new Vector2(100.0f, 100.0f);
//    private static Rect position = new Rect(0.0f, 0.0f, 640.0f, 480.0f);
}