![explain not comopetpal](Image\comopetpal.png)
التول المفروض تكون متوافقة مع Arcpro 3.3 فما اعلى

![01-BaseDimansions](Image\01-BaseDimansions.png)
محتاج يقدر يختار اكثر من parcel يجمع منهم الاطوال والعدد الموجود في البلوك

![04-Parcel count](Image\04-Parcel count.png)
محتاج يكون في preview اسف الاعداد

![07-Cornar & chamfer](Image\07-Cornar & chamfer.png)
محتاج في الستطيل اللي باللون الاحمر يظهر تفاصي وخيارت رسم الchamfer
1- ادخال جميل الاطوال طول الchamfer مع باقي الابعاد
2-اطوال الاضلال اللي على الشارع وتستنج انت طول الشطفة
3-طول الشطفة مع الزاوية
ولو عند خيار تاني ضيفة
في المستطيل التاني يكون فيه preview واقدر احدد من خلالة الparcel اللي هعمل عليها chamfer

![08-Alignment](Image\08-Alignment.png)
تأكد ان وضيفة الازارا في ال Alignment في الخيارين شغالة لانها مش غالة حاليا

![10-Validation Rules](Image\10-Validation Rules.png)
الrules is

	No invalid Geometries
	No Overlaps
	No Duplicate
	No Gaps
	No Multi Part
	No Short line - its mean legnth segment les than 10cm and  maybe ignore it
	No angle Issue - its mean angle between two segments les than 5 dgree and  maybe ignore it
	No Snap Issue - its mean any to Vertices bestance is les than 1cm must snaped
	No More Vertices - its mean angle between two segments around 180 maybe ignore it
    Must have node Vertices - its mean if corner of parcel intersect segmet must add vertix at line

i want add new step to add electric room it i will select the point to add the electric at will be on the street may all it in one parcel or between to parcel mut clip for parcels and i need add dimations for it thid step will be after chamfer
