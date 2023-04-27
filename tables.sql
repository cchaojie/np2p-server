/****** Object:  Table [dbo].[ThreejsPoseFrame]    Script Date: 2023/4/27 17:40:21 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ThreejsPoseFrame](
	[FrameId] [int] IDENTITY(1,1) NOT NULL,
	[ThreejsPoseTaskId] [uniqueidentifier] NULL,
	[FrameImage] [text] NULL,
	[FrameStatus] [nvarchar](50) NULL,
	[CreateDate] [datetime] NULL,
	[GenerateDate] [datetime] NULL,
	[FrameIndex] [int] NULL,
	[ResultImage] [text] NULL,
	[Prompt] [nvarchar](1000) NULL,
	[Seed] [nvarchar](50) NULL,
	[PaintDate] [datetime] NULL,
 CONSTRAINT [PK_ThreejsPoseFrame] PRIMARY KEY CLUSTERED 
(
	[FrameId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

/****** Object:  Table [dbo].[ThreejsPoseTask]    Script Date: 2023/4/27 17:40:22 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ThreejsPoseTask](
	[ThreejsPoseTaskId] [uniqueidentifier] NOT NULL,
	[TaskStatus] [nvarchar](50) NULL,
	[VideoPath] [nvarchar](500) NULL,
	[CreateDate] [datetime] NULL,
	[GenerateDate] [datetime] NULL,
 CONSTRAINT [PK_ThreejsPoseTask] PRIMARY KEY CLUSTERED 
(
	[ThreejsPoseTaskId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


