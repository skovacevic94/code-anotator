namespace code_anotator
{
    public class CommentAnnotation
    {
        public string SourceFile { get; set; }

        public string SourceId { get; set; }

        public Comment Comment { get; set; }

        public string CommentId { get; set; }

        public string Class { get; set; }
    }

    public enum CommentAnnotationProperties
    {
        SOURCE_ID,
        COMMENT_ID,
        LINE_NUMBER,
        TEXT,
        CLASS,
        START_INDEX,
        END_INDEX,
    }
}
