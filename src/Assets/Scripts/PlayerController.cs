using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    const float TRANS_TIME = 0.05f;//�ړ����x�J�ڎ���
    const float ROT_TIME = 0.05f;//��]�J�ڎ���

    enum RotState
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3,

        Invalid = -1,
    }

    [SerializeField] PuyoController[] _puyoControllers = new PuyoController[2] { default!, default! };
    [SerializeField] BoardController boardController = default!;

    //���Ղ�̈ʒu
    Vector2Int _position;
    //�p�x�́A0:�� 1:�E 2:�� 3:��
    RotState _rotate = RotState.Up;

    AnimationController _animationController = new();
    Vector2Int _last_position;
    RotState _last_rotate = RotState.Up;

    // Start is called before the first frame update
    void Start()
    {
        _puyoControllers[0].SetPuyoType(PuyoType.Green);
        _puyoControllers[1].SetPuyoType(PuyoType.Red);

        _position = new Vector2Int(2, 12);
        _rotate = RotState.Up;

        _puyoControllers[0].SetPos(new Vector3(_position.x, _position.y, 0.0f));
        Vector2Int posChild = CalcChildPuyoPos(_position, _rotate);
        _puyoControllers[1].SetPos(new Vector3(posChild.x, posChild.y, 0.0f));
    }

    static readonly Vector2Int[] rotate_tbl = new Vector2Int[] {
        Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left };

    private static Vector2Int CalcChildPuyoPos(Vector2Int pos, RotState rot) => pos + rotate_tbl[(int)rot];

    private bool CanMove(Vector2Int pos, RotState rot)
    {
        if (!boardController.CanSettle(pos)) return false;
        if (!boardController.CanSettle(CalcChildPuyoPos(pos, rot))) return false;

        return true;
    }

    private void SetTransiton(Vector2Int pos, RotState rot, float time)
    {
        //��Ԃ̂��߂ɕۑ�
        _last_position = _position;
        _last_rotate = _rotate;

        //�l�̍X�V
        _position = pos;
        _rotate = rot;

        _animationController.Set(time);
    }

    private bool Translate(bool is_right)
    {
        //���z�I�Ɉړ��ł��邩����
        Vector2Int pos = _position + (is_right ? Vector2Int.right : Vector2Int.left);
        if (!CanMove(pos, _rotate)) return false;

        //���ۂɈړ�
        SetTransiton(pos, _rotate, TRANS_TIME);

        return true;
    }

    private bool Rotate(bool is_right)
    {
        RotState rot = (RotState)(((int)_rotate + (is_right ? +1 : +3)) & 3);

        //���z�I�Ɉړ��ł��邩����
        Vector2Int pos = _position;
        switch (rot)
        {
            case RotState.Down:
                //�E�i���j���牺�F�����̉����E�i���j�Ƀu���b�N������Έ����オ��
                if (!boardController.CanSettle(pos + Vector2Int.down) ||
                    !boardController.CanSettle(pos + new Vector2Int(is_right ? 1 : -1, -1)))
                {
                    pos += Vector2Int.up;
                }
                break;
            case RotState.Right:
                //�E�F�E�����܂��Ă���΍��Ɉړ�
                if (!boardController.CanSettle(pos + Vector2Int.right)) pos += Vector2Int.left;
                break;
            case RotState.Left:
                //���F�������܂��Ă���ΉE�Ɉړ�
                if (!boardController.CanSettle(pos + Vector2Int.left)) pos += Vector2Int.right;
                break;
            case RotState.Up:
                break;
            default:
                Debug.Assert(false);
                break;
        }
        if (!CanMove(pos, rot)) return false;

        //���ۂɈړ�
        SetTransiton(pos, rot, ROT_TIME);

        return true;
    }

    private void QuickDrop()
    {
        //��ԉ��܂ŗ�����
        Vector2Int pos = _position;
        do
        {
            pos += Vector2Int.down;
        } while (CanMove(pos, _rotate));
        pos -= Vector2Int.down;//�ЂƂ�ɖ߂�

        _position = pos;

        //���ڐڒn
        bool is_set0 = boardController.Settle(_position,
            (int)_puyoControllers[0].GetPuyoType());
        Debug.Assert(is_set0);

        bool is_set1 = boardController.Settle(CalcChildPuyoPos(_position, _rotate),
            (int)_puyoControllers[1].GetPuyoType());
        Debug.Assert(is_set1);

        gameObject.SetActive(false);
    }

    private void Control()
    {
        //���E�ړ�
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Translate(true);
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Translate(false);
        }

        //��]
        if (Input.GetKeyDown(KeyCode.X))//�E��]
        {
            Rotate(true);
        }
        if (Input.GetKeyDown(KeyCode.Z))//����]
        {
            Rotate(false);
        }

        //�N�C�b�N�h���b�v
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            QuickDrop();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!_animationController.Update(Time.deltaTime))
        {
            Control();
        }

        float anim_rate = _animationController.GetNormalized();
        _puyoControllers[0].SetPos(Interpolate(_position, RotState.Invalid, _last_position, RotState.Invalid, anim_rate));
        _puyoControllers[1].SetPos(Interpolate(_position, _rotate, _last_position, _last_rotate, anim_rate));
    }

    //rate��1��0�� pos_last->pos,rot_last->rot�ɑJ�ځBrot��Invalid�Ȃ��]���l�����Ȃ��i���Ղ�p�j
    static Vector3 Interpolate(Vector2Int pos, RotState rot, Vector2Int pos_last, RotState rot_last, float rate)
    {
        //���s�ړ�
        Vector3 p = Vector3.Lerp(
            new Vector3(pos.x, pos.y, 0.0f),
            new Vector3(pos_last.x, pos_last.y, 0.0f), rate);

        if (rot == RotState.Invalid) return p;

        //��]
        float theta0 = 0.5f * Mathf.PI * (int)rot;
        float theta1 = 0.5f * Mathf.PI * (int)rot_last;
        float theta = theta1 - theta0;

        //�߂����ɉ��
        if (+Mathf.PI < theta) theta -= 2.0f * Mathf.PI;
        if (theta < -Mathf.PI) theta += 2.0f * Mathf.PI;

        theta = theta0 + rate * theta;

        return p + new Vector3(Mathf.Sin(theta), Mathf.Cos(theta), 0.0f);
    }
}