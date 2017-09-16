﻿Перем юТест;

Функция Версия() Экспорт
	Возврат "0.1";
КонецФункции

Функция ПолучитьСписокТестов(ЮнитТестирование) Экспорт

	юТест = ЮнитТестирование;

	ВсеТесты = Новый Массив;

	СИ = Новый СистемнаяИнформация;
	Если (Найти(СИ.ВерсияОС, "Windows") > 0) И Не (ПеременныеСреды().Получить("APPVEYOR") = "True") Тогда
		ВсеТесты.Добавить("ТестДолжен_ПроверитьУстановкуЧисловыхСвойств");
		ВсеТесты.Добавить("ТестДолжен_ПроверитьВызовСОпциональнымиПараметрами");
	КонецЕсли;

	ВсеТесты.Добавить("ТестДолжен_ПроверитьСозданиеClrОбъекта");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьСозданиеClrКоллекции");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьСозданиеClrКоллекцииШаблона");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьТипыClrОбъектов");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьРаботуGetType");

	Возврат ВсеТесты;
КонецФункции

Процедура ТестДолжен_ПроверитьУстановкуЧисловыхСвойств() Экспорт

	conn = Новый ComObject("ADODB.Connection");
	conn.ConnectionString = "Driver={MySQL ODBC 3.51 Driver}; Server=obr-mdb-01; Port=3306; UID=v8; PWD=G0bl1n76; database=db1cprod; option=3;";
	conn.CommandTimeOut= 30;

КонецПроцедуры

Процедура ТестДолжен_ПроверитьВызовСОпциональнымиПараметрами() Экспорт

	conn = Новый ComObject("ADODB.Connection");
	conn.ConnectionString = "Driver={fake driver}; Server=obr-mdb-01; Port=3306; UID=v8; PWD=G0bl1n76; database=db1cprod; option=3;";
	conn.CommandTimeOut= 30;

	Попытка
		conn.Open(,,);
	Исключение
		Инфо = ИнформацияОбОшибке().Причина;
		Если Инфо = Неопределено Тогда
			Инфо = ИнформацияОбОшибке();
		КонецЕсли;

		юТест.ПроверитьИстину(Найти(Строка(Инфо),"[Microsoft]") > 0, ИнформацияОбОшибке().Описание);
	КонецПопытки;

КонецПроцедуры

Процедура ТестДолжен_ПроверитьСозданиеClrОбъекта() Экспорт

	РоднойОбъект = Новый COMОбъект("System.Random");
	РоднойОбъект.Next();
	РоднойОбъект.Next(10);
	РоднойОбъект.Next(10, 20);
	РоднойОбъект.NextDouble();

КонецПроцедуры

Процедура ТестДолжен_ПроверитьСозданиеClrКоллекции() Экспорт

	РоднаяКоллекцияНеШаблон = Новый COMОбъект("System.Collections.ArrayList");
	РоднаяКоллекцияНеШаблон.Add(1);
	РоднаяКоллекцияНеШаблон.Add("string");
	РоднаяКоллекцияНеШаблон.Add('20170808');

	юТест.ПроверитьРавенство(РоднаяКоллекцияНеШаблон.Count, 3);
	юТест.ПроверитьРавенство(РоднаяКоллекцияНеШаблон[0], 1);
	юТест.ПроверитьРавенство(РоднаяКоллекцияНеШаблон[1], "string");
	юТест.ПроверитьРавенство(РоднаяКоллекцияНеШаблон[2], '20170808');

КонецПроцедуры

Процедура ТестДолжен_ПроверитьСозданиеClrКоллекцииШаблона() Экспорт

	РоднаяКоллекцияШаблон = Новый COMОбъект("System.Collections.Generic.List`1");
	РоднаяКоллекцияШаблон.Add(1);
	РоднаяКоллекцияШаблон.Add("string");
	РоднаяКоллекцияШаблон.Add('20170808');

	юТест.ПроверитьРавенство(РоднаяКоллекцияШаблон.Count, 3);
	юТест.ПроверитьРавенство(РоднаяКоллекцияШаблон[0], 1);
	юТест.ПроверитьРавенство(РоднаяКоллекцияШаблон[1], "string");
	юТест.ПроверитьРавенство(РоднаяКоллекцияШаблон[2], '20170808');

КонецПроцедуры

Процедура ТестДолжен_ПроверитьТипыClrОбъектов() Экспорт

	РодноеСоответствие = Новый ComОбъект("System.Collections.Generic.Dictionary`2");
	РодноеСоответствие.Add("str", 123);

	юТест.ПроверитьРавенство  (ТипЗнч(РодноеСоответствие), Тип("COMОбъект"), "CLR-тип как COM-объект");

	Для Каждого мКлючЗначение Из РодноеСоответствие Цикл
		юТест.ПроверитьРавенство(ТипЗнч(мКлючЗначение), Тип("COMОбъект"), "CLR-тип как COM-объект");
	КонецЦикла;

КонецПроцедуры

Процедура РазделитьТипИПространствоИмен(Знач ПолноеИмя, ПространствоИмен, Имя)

	М = СтрРазделить(ПолноеИмя, ".");
	Имя = М.Получить(М.ВГраница());

	М.Удалить(М.ВГраница());

	ПространствоИмен = СтрСоединить(М, ".");

КонецПроцедуры

Процедура ПроверитьПолучениеТипаКомОбъекта(Знач ИмяПроверяемогоТипа)

	Перем ПространствоИмен, Имя;

	РазделитьТипИПространствоИмен(ИмяПроверяемогоТипа, ПространствоИмен, Имя);
	
	Объект = Новый COMОбъект(ИмяПроверяемогоТипа);
	Рефлектор = Новый Рефлектор;

	юТест.ПроверитьИстину(Рефлектор.МетодСуществует(Объект, "GetType"), "Существует метод `GetType()`");
	
	ТипОбъекта = Объект.GetType();

	юТест.ПроверитьРавенство(ТипЗнч(ТипОбъекта), Тип("COMОбъект"), "Тип - COM-объект");

	юТест.ПроверитьРавенство(ВРЕГ(ПространствоИмен), ВРЕГ(ТипОбъекта.Namespace), "Получили тот тип, который просили");
	юТест.ПроверитьРавенство(ВРЕГ(Имя), ВРЕГ(ТипОбъекта.Name), "Получили тот тип, который просили");

КонецПроцедуры

Процедура ТестДолжен_ПроверитьРаботуGetType() Экспорт

	ПроверитьПолучениеТипаКомОбъекта("System.Random");
	ПроверитьПолучениеТипаКомОбъекта("System.Collections.ArrayList");
	ПроверитьПолучениеТипаКомОбъекта("System.Collections.Generic.List`1");
	ПроверитьПолучениеТипаКомОбъекта("System.Collections.Generic.Dictionary`2");
	
КонецПроцедуры
