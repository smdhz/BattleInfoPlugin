namespace BattleInfoPlugin
{
    /// <summary>
    /// �ʒm�̎�ނ������ÓI�����o�[�����J���܂��B
    /// </summary>
    public static class NotificationType
    {
        private static readonly string baseName = typeof(NotificationType).Assembly.GetName().Name;
        /// <summary>
        /// �퓬�I�����̒ʒm�����ʂ��邽�߂̕�������擾���܂��B
        /// </summary>
        public static string BattleEnd = $"{baseName}.{nameof(BattleEnd)}";
    }
}